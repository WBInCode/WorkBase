using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using WorkBase.Contracts;
using WorkBase.Infrastructure.Persistence;
using WorkBase.Modules.Notification.Domain.Entities;
using WorkBase.Modules.Tasks.Domain.Events;

namespace WorkBase.Infrastructure.Tasks;

/// <summary>
/// Powiadamia osobę odpowiedzialną, że zadanie przekroczyło termin.
/// </summary>
/// <remarks>
/// <para>
/// <c>TaskOverdueDetectorJob</c> chodził codziennie o 06:00 i publikował <c>TaskOverdueEvent</c>,
/// którego <b>nikt nie obsługiwał</b> — zadanie pracowało w próżnię od początku istnienia.
/// </para>
/// <para>
/// Klasa siedzi w <c>WorkBase.Infrastructure</c>, a nie w module zadań, bo spina dwa moduły:
/// zdarzenie z Tasks i powiadomienia z Notification. Ten sam wzorzec co
/// <c>ZamknijWniosekPoObiegu</c>.
/// </para>
/// <para>
/// <b>Wysyłamy raz na zadanie, nie raz dziennie.</b> Job publikuje zdarzenie przy KAŻDYM
/// przebiegu dla każdego zaległego zadania, więc bez tego zabezpieczenia zadanie zaległe
/// miesiąc dałoby trzydzieści powiadomień. Sprawdzamy, czy powiadomienie tej kategorii dla
/// tego zadania już istnieje — po to właśnie encja powiadomienia ma <c>ReferenceType</c>
/// i <c>ReferenceId</c>.
/// </para>
/// </remarks>
public sealed class PowiadomOZaleglymZadaniu(
    WorkBaseDbContext dbContext,
    IOrganizationLookupService organizationLookup,
    INotificationService notificationService,
    ILogger<PowiadomOZaleglymZadaniu> logger)
    : INotificationHandler<TaskOverdueEvent>
{
    private const string Kategoria = "task_overdue";
    private const string TypEncji = "task";

    public async Task Handle(TaskOverdueEvent notification, CancellationToken cancellationToken)
    {
        if (notification.AssigneeId == Guid.Empty) return;

        var juzWyslane = await dbContext.Set<Notification>()
            .IgnoreQueryFilters()
            .AnyAsync(
                p => p.TenantId == notification.TenantId
                    && p.Category == Kategoria
                    && p.ReferenceType == TypEncji
                    && p.ReferenceId == notification.TaskId,
                cancellationToken);
        if (juzWyslane) return;

        // Dzwonek pyta o powiadomienia identyfikatorem konta, wiec adresujemy je kontem,
        // a nie identyfikatorem pracownika.
        var userId = await organizationLookup.GetUserIdByEmployeeIdAsync(
            notification.AssigneeId, cancellationToken);
        if (userId is null)
        {
            logger.LogDebug(
                "Pracownik {EmployeeId} nie ma konta — pomijam powiadomienie o zaległym zadaniu {TaskId}.",
                notification.AssigneeId, notification.TaskId);
            return;
        }

        var dni = (int)Math.Floor((DateTime.UtcNow - notification.DueDate).TotalDays);
        var opis = dni <= 0
            ? "Termin minął dzisiaj."
            : $"Termin minął {dni} {(dni == 1 ? "dzień" : "dni")} temu.";

        await notificationService.SendAsync(
            notification.TenantId,
            userId.Value,
            "Zadanie po terminie",
            $"{notification.Title} — {opis}",
            Kategoria,
            TypEncji,
            notification.TaskId,
            cancellationToken);
    }
}

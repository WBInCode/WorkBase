using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using WorkBase.Infrastructure.Persistence;
using WorkBase.Modules.Leave.Domain.Entities;
using WorkBase.Modules.Workflow.Domain.Events;

namespace WorkBase.Infrastructure.Leave;

/// <summary>
/// Domyka wniosek urlopowy po decyzji przełożonego. Silnik obiegu kończył instancję,
/// ale nikt nie przenosił tego na sam wniosek — zostawał w stanie „Oczekuje" na zawsze,
/// saldo trzymało dni jako zarezerwowane, a kalendarz nieobecności pozostawał pusty.
/// Handler leży w Infrastructure, bo łączy dwa moduły (Leave + Workflow).
/// </summary>
public sealed class ZamknijWniosekUrlopowyPoObiegu(
    WorkBaseDbContext dbContext,
    ILogger<ZamknijWniosekUrlopowyPoObiegu> logger)
    : INotificationHandler<WorkflowInstanceCompletedEvent>,
      INotificationHandler<WorkflowInstanceRejectedEvent>
{
    private const string TypEncji = "LeaveRequest";

    public async Task Handle(WorkflowInstanceCompletedEvent notification, CancellationToken cancellationToken)
    {
        if (!string.Equals(notification.EntityType, TypEncji, StringComparison.OrdinalIgnoreCase)) return;

        var wniosek = await PobierzAsync(notification.EntityId, cancellationToken);
        if (wniosek is null || wniosek.Status != LeaveRequestStatus.Pending) return;

        wniosek.Approve();
        await PrzeniesSaldoAsync(wniosek, zatwierdzony: true, cancellationToken);
        await ZapiszKalendarzAsync(wniosek, cancellationToken);

        await dbContext.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Wniosek urlopowy {RequestId} zatwierdzony po decyzji w obiegu", wniosek.Id);
    }

    public async Task Handle(WorkflowInstanceRejectedEvent notification, CancellationToken cancellationToken)
    {
        if (!string.Equals(notification.EntityType, TypEncji, StringComparison.OrdinalIgnoreCase)) return;

        var wniosek = await PobierzAsync(notification.EntityId, cancellationToken);
        if (wniosek is null || wniosek.Status != LeaveRequestStatus.Pending) return;

        wniosek.Reject();
        await PrzeniesSaldoAsync(wniosek, zatwierdzony: false, cancellationToken);

        await dbContext.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Wniosek urlopowy {RequestId} odrzucony po decyzji w obiegu", wniosek.Id);
    }

    private Task<LeaveRequest?> PobierzAsync(Guid id, CancellationToken ct) =>
        dbContext.Set<LeaveRequest>().IgnoreQueryFilters().FirstOrDefaultAsync(r => r.Id == id, ct);

    private async Task PrzeniesSaldoAsync(LeaveRequest wniosek, bool zatwierdzony, CancellationToken ct)
    {
        var rok = wniosek.StartDate.Year;
        var saldo = await dbContext.Set<LeaveBalance>()
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(
                b => b.TenantId == wniosek.TenantId
                    && b.EmployeeId == wniosek.EmployeeId
                    && b.LeaveTypeId == wniosek.LeaveTypeId
                    && b.Year == rok,
                ct);
        if (saldo is null) return;

        if (zatwierdzony)
            saldo.ConfirmUsed(wniosek.TotalDays);
        else
            saldo.RemovePending(wniosek.TotalDays);
    }

    private async Task ZapiszKalendarzAsync(LeaveRequest wniosek, CancellationToken ct)
    {
        var maJuz = await dbContext.Set<LeaveCalendarEntry>()
            .IgnoreQueryFilters()
            .AnyAsync(e => e.LeaveRequestId == wniosek.Id, ct);
        if (maJuz) return;

        for (var dzien = wniosek.StartDate.Date; dzien <= wniosek.EndDate.Date; dzien = dzien.AddDays(1))
        {
            if (dzien.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday) continue;

            dbContext.Set<LeaveCalendarEntry>().Add(LeaveCalendarEntry.Create(
                wniosek.TenantId,
                wniosek.EmployeeId,
                wniosek.Id,
                wniosek.LeaveTypeId,
                DateTime.SpecifyKind(dzien, DateTimeKind.Utc)));
        }
    }
}

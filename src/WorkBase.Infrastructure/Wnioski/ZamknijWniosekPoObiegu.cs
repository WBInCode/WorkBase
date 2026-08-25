using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using WorkBase.Infrastructure.Persistence;
using WorkBase.Modules.Workflow.Domain.Entities;
using WorkBase.Modules.Workflow.Domain.Events;

namespace WorkBase.Infrastructure.Wnioski;

/// <summary>
/// Domyka wniosek firmowy po decyzji przełożonego.
/// </summary>
/// <remarks>
/// Silnik kończy instancję obiegu, ale sam wniosek zostałby w stanie „Oczekuje” na zawsze —
/// dokładnie ten błąd wystąpił wcześniej przy wnioskach urlopowych, więc powtarzamy tu
/// sprawdzone rozwiązanie zamiast wymyślać nowe.
///
/// Handler leży w Infrastructure, bo choć dziś dotyka tylko encji modułu Workflow, to jest
/// spinaczem między silnikiem a stanem wniosku — tak samo jak jego odpowiednik dla urlopów.
/// </remarks>
public sealed class ZamknijWniosekPoObiegu(
    WorkBaseDbContext dbContext,
    ILogger<ZamknijWniosekPoObiegu> logger)
    : INotificationHandler<WorkflowInstanceCompletedEvent>,
      INotificationHandler<WorkflowInstanceRejectedEvent>
{
    public async Task Handle(WorkflowInstanceCompletedEvent notification, CancellationToken cancellationToken)
        => await RozstrzygnijAsync(notification.EntityType, notification.InstanceId, zaakceptowany: true, cancellationToken);

    public async Task Handle(WorkflowInstanceRejectedEvent notification, CancellationToken cancellationToken)
        => await RozstrzygnijAsync(notification.EntityType, notification.InstanceId, zaakceptowany: false, cancellationToken);

    private async Task RozstrzygnijAsync(
        string entityType, Guid instanceId, bool zaakceptowany, CancellationToken cancellationToken)
    {
        if (!string.Equals(entityType, Wniosek.TypEncjiWObiegu, StringComparison.OrdinalIgnoreCase)) return;

        var wniosek = await dbContext.Set<Wniosek>()
            .FirstOrDefaultAsync(w => w.WorkflowInstanceId == instanceId, cancellationToken);

        if (wniosek is null) return;

        // Wniosek wycofany przez skladajacego juz nie czeka na decyzje — obieg moze sie domknac
        // pozniej, ale nie moze cofnac tamtej decyzji.
        var wynik = zaakceptowany ? wniosek.Zaakceptuj() : wniosek.Odrzuc();
        if (wynik.IsFailure) return;

        await dbContext.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Wniosek {WniosekId} rozstrzygniety po decyzji w obiegu: {Status}",
            wniosek.Id, wniosek.Status);
    }
}

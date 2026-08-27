using MediatR;
using Microsoft.Extensions.Logging;
using WorkBase.Contracts;
using WorkBase.Modules.Tasks.Application.Contracts;
using WorkBase.Modules.Tasks.Domain.Events;

namespace WorkBase.Modules.Tasks.Application.EventHandlers;

/// <summary>
/// Powiadamia osobę, na którą trafiło zadanie — przy utworzeniu i przy późniejszym przepisaniu.
/// Powiadomienie idzie na dzwonek w WorkBase i, przez Hub, na dzwonek w WB Platform.
/// </summary>
public sealed class TaskAssignedNotificationHandler(
    ITaskItemRepository taskRepository,
    IOrganizationLookupService organizationLookup,
    INotificationService notificationService,
    ILogger<TaskAssignedNotificationHandler> logger)
    : INotificationHandler<TaskCreatedEvent>, INotificationHandler<TaskAssignedEvent>
{
    public Task Handle(TaskCreatedEvent notification, CancellationToken cancellationToken) =>
        NotifyAsync(notification.TenantId, notification.TaskId, notification.AssigneeId,
            notification.Title, cancellationToken);

    public async Task Handle(TaskAssignedEvent notification, CancellationToken cancellationToken)
    {
        if (notification.NewAssigneeId == notification.OldAssigneeId)
            return;

        var task = await taskRepository.GetByIdAsync(notification.TaskId, cancellationToken);
        if (task is null)
            return;

        await NotifyAsync(notification.TenantId, notification.TaskId, notification.NewAssigneeId,
            task.Title, cancellationToken);
    }

    private async Task NotifyAsync(
        Guid tenantId, Guid taskId, Guid assigneeId, string title, CancellationToken cancellationToken)
    {
        if (assigneeId == Guid.Empty)
            return;

        // Dzwonek pyta o powiadomienia identyfikatorem konta, wiec adresujemy je kontem,
        // a nie identyfikatorem pracownika.
        var userId = await organizationLookup.GetUserIdByEmployeeIdAsync(assigneeId, cancellationToken);
        if (userId is null)
        {
            logger.LogDebug(
                "Pracownik {EmployeeId} nie ma konta — pomijam powiadomienie o zadaniu {TaskId}.",
                assigneeId, taskId);
            return;
        }

        await notificationService.SendFromTemplateAsync(
            tenantId,
            userId.Value,
            templateCode: "task_assigned",
            variables: new Dictionary<string, string?> { ["tytul"] = title },
            fallbackTitle: "Nowe zadanie",
            fallbackBody: title,
            category: "task_assigned",
            referenceType: "task",
            referenceId: taskId,
            ct: cancellationToken);
    }
}

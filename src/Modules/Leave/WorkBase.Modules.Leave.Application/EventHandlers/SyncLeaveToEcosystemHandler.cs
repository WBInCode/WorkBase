using MediatR;
using WorkBase.Contracts.Ecosystem;
using WorkBase.Modules.Leave.Domain.Events;

namespace WorkBase.Modules.Leave.Application.EventHandlers;

public sealed class SyncLeaveToEcosystemHandler(IEcosystemSyncScheduler scheduler) :
    INotificationHandler<LeaveRequestApprovedEvent>,
    INotificationHandler<LeaveRequestRejectedEvent>,
    INotificationHandler<LeaveRequestCancelledEvent>
{
    public Task Handle(LeaveRequestApprovedEvent notification, CancellationToken cancellationToken)
    {
        scheduler.Enqueue(notification.TenantId, notification.EmployeeId);
        return Task.CompletedTask;
    }

    public Task Handle(LeaveRequestRejectedEvent notification, CancellationToken cancellationToken)
    {
        scheduler.Enqueue(notification.TenantId, notification.EmployeeId);
        return Task.CompletedTask;
    }

    public Task Handle(LeaveRequestCancelledEvent notification, CancellationToken cancellationToken)
    {
        scheduler.Enqueue(notification.TenantId, notification.EmployeeId);
        return Task.CompletedTask;
    }
}
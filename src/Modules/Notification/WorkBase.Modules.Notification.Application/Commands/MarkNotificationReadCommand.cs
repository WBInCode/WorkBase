using WorkBase.Modules.Notification.Application.Contracts;
using WorkBase.Shared.Cqrs;
using WorkBase.Shared.Domain;

namespace WorkBase.Modules.Notification.Application.Commands;

public sealed record MarkNotificationReadCommand(Guid NotificationId) : ICommand, ITenantRequest
{
    public Guid TenantId { get; set; }
}

public sealed class MarkNotificationReadHandler(INotificationRepository repository)
    : ICommandHandler<MarkNotificationReadCommand>
{
    public async Task<Result> Handle(MarkNotificationReadCommand request, CancellationToken cancellationToken)
    {
        var notification = await repository.GetByIdAsync(request.NotificationId, cancellationToken);
        if (notification is null)
            return Result.Failure(Error.NotFound("Notification.NotFound", "Notification not found"));

        if (notification.TenantId != request.TenantId)
            return Result.Failure(Error.NotFound("Notification.NotFound", "Notification not found"));

        notification.MarkAsRead();
        repository.Update(notification);
        await repository.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}

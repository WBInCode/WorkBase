using WorkBase.Modules.Notification.Application.Contracts;
using WorkBase.Shared.Cqrs;
using WorkBase.Shared.Domain;

namespace WorkBase.Modules.Notification.Application.Commands;

public sealed record MarkAllNotificationsReadCommand(Guid RecipientUserId) : ICommand, ITenantRequest
{
    public Guid TenantId { get; set; }
}

public sealed class MarkAllNotificationsReadHandler(INotificationRepository repository)
    : ICommandHandler<MarkAllNotificationsReadCommand>
{
    public async Task<Result> Handle(MarkAllNotificationsReadCommand request, CancellationToken cancellationToken)
    {
        await repository.MarkAllReadAsync(request.TenantId, request.RecipientUserId, cancellationToken);
        await repository.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}

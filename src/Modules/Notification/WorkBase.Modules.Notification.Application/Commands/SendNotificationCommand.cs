using WorkBase.Modules.Notification.Application.Contracts;
using WorkBase.Shared.Cqrs;
using WorkBase.Shared.Domain;

namespace WorkBase.Modules.Notification.Application.Commands;

public sealed record SendNotificationCommand(
    Guid RecipientUserId,
    string Title,
    string Body,
    string Category,
    string? ReferenceType = null,
    Guid? ReferenceId = null) : ICommand<Guid>, ITenantRequest
{
    public Guid TenantId { get; set; }
}

public sealed class SendNotificationHandler(INotificationRepository repository)
    : ICommandHandler<SendNotificationCommand, Guid>
{
    public async Task<Result<Guid>> Handle(SendNotificationCommand request, CancellationToken cancellationToken)
    {
        var notification = Domain.Entities.Notification.Create(
            request.TenantId,
            request.RecipientUserId,
            request.Title,
            request.Body,
            request.Category,
            request.ReferenceType,
            request.ReferenceId);

        await repository.AddAsync(notification, cancellationToken);
        await repository.SaveChangesAsync(cancellationToken);

        return notification.Id;
    }
}

using WorkBase.Modules.Notification.Application.Contracts;
using WorkBase.Shared.Cqrs;
using WorkBase.Shared.Domain;

namespace WorkBase.Modules.Notification.Application.Queries;

public sealed record NotificationDto(
    Guid Id,
    string Title,
    string Body,
    string Category,
    bool IsRead,
    DateTime CreatedAt,
    DateTime? ReadAt,
    string? ReferenceType,
    Guid? ReferenceId);

public sealed record GetNotificationsQuery(Guid RecipientUserId, bool UnreadOnly = false, int Limit = 50)
    : IQuery<List<NotificationDto>>, ITenantRequest
{
    public Guid TenantId { get; set; }
}

public sealed class GetNotificationsHandler(INotificationRepository repository)
    : IQueryHandler<GetNotificationsQuery, List<NotificationDto>>
{
    public async Task<Result<List<NotificationDto>>> Handle(GetNotificationsQuery request, CancellationToken cancellationToken)
    {
        var notifications = await repository.GetByRecipientAsync(
            request.TenantId, request.RecipientUserId, request.UnreadOnly, request.Limit, cancellationToken);

        var dtos = notifications.Select(n => new NotificationDto(
            n.Id,
            n.Title,
            n.Body,
            n.Category,
            n.IsRead,
            n.CreatedAt,
            n.ReadAt,
            n.ReferenceType,
            n.ReferenceId)).ToList();

        return dtos;
    }
}

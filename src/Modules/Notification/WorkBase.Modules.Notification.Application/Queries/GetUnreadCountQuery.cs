using WorkBase.Modules.Notification.Application.Contracts;
using WorkBase.Shared.Cqrs;
using WorkBase.Shared.Domain;

namespace WorkBase.Modules.Notification.Application.Queries;

public sealed record GetUnreadCountQuery(Guid RecipientUserId) : IQuery<int>, ITenantRequest
{
    public Guid TenantId { get; set; }
}

public sealed class GetUnreadCountHandler(INotificationRepository repository)
    : IQueryHandler<GetUnreadCountQuery, int>
{
    public async Task<Result<int>> Handle(GetUnreadCountQuery request, CancellationToken cancellationToken)
    {
        var count = await repository.GetUnreadCountAsync(request.TenantId, request.RecipientUserId, cancellationToken);
        return count;
    }
}

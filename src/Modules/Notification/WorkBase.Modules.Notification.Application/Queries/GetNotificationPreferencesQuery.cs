using WorkBase.Modules.Notification.Application.Contracts;
using WorkBase.Shared.Cqrs;
using WorkBase.Shared.Domain;

namespace WorkBase.Modules.Notification.Application.Queries;

public sealed record NotificationPreferenceDto(
    Guid Id, string Category, bool InApp, bool Email);

public sealed record GetNotificationPreferencesQuery(Guid UserId) : IQuery<List<NotificationPreferenceDto>>, ITenantRequest
{
    public Guid TenantId { get; set; }
}

public sealed class GetNotificationPreferencesHandler(INotificationPreferenceRepository repository)
    : IQueryHandler<GetNotificationPreferencesQuery, List<NotificationPreferenceDto>>
{
    public async Task<Result<List<NotificationPreferenceDto>>> Handle(
        GetNotificationPreferencesQuery request, CancellationToken cancellationToken)
    {
        var prefs = await repository.GetByUserAsync(request.TenantId, request.UserId, cancellationToken);
        return prefs.Select(p => new NotificationPreferenceDto(
            p.Id, p.Category, p.InApp, p.Email)).ToList();
    }
}

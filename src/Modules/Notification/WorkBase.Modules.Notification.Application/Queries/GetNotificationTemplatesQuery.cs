using WorkBase.Modules.Notification.Application.Contracts;
using WorkBase.Shared.Cqrs;
using WorkBase.Shared.Domain;

namespace WorkBase.Modules.Notification.Application.Queries;

public sealed record NotificationTemplateDto(
    Guid Id, string Code, string Name,
    string TitleTemplate, string BodyTemplate,
    string Category, bool IsActive);

public sealed record GetNotificationTemplatesQuery() : IQuery<List<NotificationTemplateDto>>, ITenantRequest
{
    public Guid TenantId { get; set; }
}

public sealed class GetNotificationTemplatesHandler(INotificationTemplateRepository repository)
    : IQueryHandler<GetNotificationTemplatesQuery, List<NotificationTemplateDto>>
{
    public async Task<Result<List<NotificationTemplateDto>>> Handle(
        GetNotificationTemplatesQuery request, CancellationToken cancellationToken)
    {
        var templates = await repository.GetByTenantAsync(request.TenantId, cancellationToken);
        return templates.Select(t => new NotificationTemplateDto(
            t.Id, t.Code, t.Name, t.TitleTemplate, t.BodyTemplate,
            t.Category, t.IsActive)).ToList();
    }
}

using WorkBase.Modules.TimeTracking.Application.Dtos;
using WorkBase.Shared.Cqrs;

namespace WorkBase.Modules.TimeTracking.Application.Queries;

public sealed record GetScheduleTemplatesQuery()
    : IQuery<IReadOnlyList<ScheduleTemplateDto>>, ITenantRequest
{
    public Guid TenantId { get; set; }
}

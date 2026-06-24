using WorkBase.Modules.TimeTracking.Application.Dtos;
using WorkBase.Shared.Cqrs;

namespace WorkBase.Modules.TimeTracking.Application.Queries;

public sealed record GetOrgUnitScheduleQuery(Guid OrgUnitId) : IQuery<OrgUnitScheduleDto?>, ITenantRequest
{
    public Guid TenantId { get; set; }
}

public sealed record GetOrgUnitSchedulesQuery() : IQuery<IReadOnlyList<OrgUnitScheduleDto>>, ITenantRequest
{
    public Guid TenantId { get; set; }
}

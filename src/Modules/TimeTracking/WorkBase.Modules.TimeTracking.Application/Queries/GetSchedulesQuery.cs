using WorkBase.Modules.TimeTracking.Application.Dtos;
using WorkBase.Shared.Cqrs;

namespace WorkBase.Modules.TimeTracking.Application.Queries;

public sealed record GetSchedulesQuery(
    Guid EmployeeId,
    DateOnly From,
    DateOnly To) : IQuery<IReadOnlyList<ScheduleDto>>, ITenantRequest
{
    public Guid TenantId { get; set; }
}

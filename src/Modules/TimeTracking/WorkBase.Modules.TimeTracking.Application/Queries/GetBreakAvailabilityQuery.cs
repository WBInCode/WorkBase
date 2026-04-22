using WorkBase.Modules.TimeTracking.Application.Dtos;
using WorkBase.Shared.Cqrs;

namespace WorkBase.Modules.TimeTracking.Application.Queries;

public sealed record GetBreakAvailabilityQuery(Guid EmployeeId) : IQuery<BreakAvailabilityDto>, ITenantRequest
{
    public Guid TenantId { get; set; }
}

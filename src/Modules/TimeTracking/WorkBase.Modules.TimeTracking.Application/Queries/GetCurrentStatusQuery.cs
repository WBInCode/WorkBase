using WorkBase.Modules.TimeTracking.Application.Dtos;
using WorkBase.Shared.Cqrs;

namespace WorkBase.Modules.TimeTracking.Application.Queries;

public sealed record GetCurrentStatusQuery(
    Guid EmployeeId) : IQuery<TimeStatusDto>, ITenantRequest
{
    public Guid TenantId { get; set; }
}

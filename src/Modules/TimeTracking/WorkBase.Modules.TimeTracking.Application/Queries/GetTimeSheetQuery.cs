using WorkBase.Modules.TimeTracking.Application.Dtos;
using WorkBase.Shared.Cqrs;

namespace WorkBase.Modules.TimeTracking.Application.Queries;

public sealed record GetTimeSheetQuery(
    Guid EmployeeId,
    DateOnly From,
    DateOnly To,
    string Period = "day") : IQuery<TimeSheetPeriodDto>, ITenantRequest
{
    public Guid TenantId { get; set; }
}

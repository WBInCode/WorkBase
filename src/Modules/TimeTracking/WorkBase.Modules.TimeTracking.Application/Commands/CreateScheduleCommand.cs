using WorkBase.Shared.Cqrs;

namespace WorkBase.Modules.TimeTracking.Application.Commands;

public sealed record CreateScheduleCommand(
    Guid EmployeeId,
    DateOnly Date,
    TimeOnly PlannedStart,
    TimeOnly PlannedEnd,
    string? ShiftType = null,
    Guid? TemplateId = null) : ICommand<Guid>, ITenantRequest
{
    public Guid TenantId { get; set; }
}

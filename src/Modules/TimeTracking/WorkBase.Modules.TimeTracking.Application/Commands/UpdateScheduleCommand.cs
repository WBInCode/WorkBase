using WorkBase.Shared.Cqrs;

namespace WorkBase.Modules.TimeTracking.Application.Commands;

public sealed record UpdateScheduleCommand(
    Guid ScheduleId,
    TimeOnly PlannedStart,
    TimeOnly PlannedEnd,
    string? ShiftType = null) : ICommand, ITenantRequest
{
    public Guid TenantId { get; set; }
}

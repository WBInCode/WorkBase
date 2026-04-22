using WorkBase.Shared.Cqrs;
using WorkBase.Shared.Domain;

namespace WorkBase.Modules.TimeTracking.Application.Commands;

public sealed record DayShiftPattern(
    DayOfWeek DayOfWeek,
    TimeOnly PlannedStart,
    TimeOnly PlannedEnd,
    string? ShiftType = null,
    Guid? TemplateId = null);

public sealed record GenerateBatchSchedulesCommand(
    IReadOnlyList<Guid> EmployeeIds,
    DateOnly From,
    DateOnly To,
    IReadOnlyList<DayShiftPattern> WeekPattern,
    bool Overwrite = false) : ICommand<int>, ITenantRequest
{
    public Guid TenantId { get; set; }
}

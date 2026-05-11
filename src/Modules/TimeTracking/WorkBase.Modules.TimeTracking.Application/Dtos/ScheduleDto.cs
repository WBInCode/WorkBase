namespace WorkBase.Modules.TimeTracking.Application.Dtos;

public sealed record ScheduleDto(
    Guid Id,
    Guid EmployeeId,
    DateOnly Date,
    TimeOnly PlannedStart,
    TimeOnly PlannedEnd,
    string? ShiftType,
    Guid? TemplateId,
    TimeSpan PlannedDuration,
    string Source = "Individual",
    Guid? OrgUnitScheduleId = null);

public sealed record ScheduleTemplateDto(
    Guid Id,
    string Name,
    string? Description,
    string Definition,
    bool IsActive);

public sealed record OrgUnitScheduleDto(
    Guid Id,
    Guid OrgUnitId,
    string Name,
    string WeekPattern,
    DateOnly EffectiveFrom,
    bool IsActive);

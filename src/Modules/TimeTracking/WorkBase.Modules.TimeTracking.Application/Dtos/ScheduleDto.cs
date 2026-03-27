namespace WorkBase.Modules.TimeTracking.Application.Dtos;

public sealed record ScheduleDto(
    Guid Id,
    Guid EmployeeId,
    DateOnly Date,
    TimeOnly PlannedStart,
    TimeOnly PlannedEnd,
    string? ShiftType,
    Guid? TemplateId,
    TimeSpan PlannedDuration);

public sealed record ScheduleTemplateDto(
    Guid Id,
    string Name,
    string? Description,
    string Definition,
    bool IsActive);

namespace WorkBase.Modules.Leave.Application.Dtos;

public sealed record LeaveTypeDto(
    Guid Id,
    string Code,
    string Name,
    string? Description,
    bool IsPaid,
    bool RequiresApproval,
    int? DefaultDaysPerYear,
    string? Color,
    int SortOrder);

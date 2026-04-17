namespace WorkBase.Modules.Leave.Application.Dtos;

public sealed record LeaveRequestDto(
    Guid Id,
    Guid EmployeeId,
    Guid LeaveTypeId,
    string LeaveTypeCode,
    string LeaveTypeName,
    string? LeaveTypeColor,
    DateTime StartDate,
    DateTime EndDate,
    decimal TotalDays,
    string Status,
    string? Reason,
    DateTime CreatedAt);

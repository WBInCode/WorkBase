namespace WorkBase.Modules.Leave.Application.Dtos;

public sealed record LeaveCalendarEntryDto(
    Guid EmployeeId,
    Guid LeaveTypeId,
    string LeaveTypeCode,
    string LeaveTypeName,
    string? LeaveTypeColor,
    DateTime Date,
    decimal DayFraction);

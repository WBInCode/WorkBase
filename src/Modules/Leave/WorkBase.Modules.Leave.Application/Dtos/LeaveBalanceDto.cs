namespace WorkBase.Modules.Leave.Application.Dtos;

public sealed record LeaveBalanceDto(
    Guid Id,
    Guid LeaveTypeId,
    string LeaveTypeCode,
    string LeaveTypeName,
    string? LeaveTypeColor,
    int Year,
    decimal TotalDays,
    decimal UsedDays,
    decimal PendingDays,
    decimal CarriedOverDays,
    decimal RemainingDays);

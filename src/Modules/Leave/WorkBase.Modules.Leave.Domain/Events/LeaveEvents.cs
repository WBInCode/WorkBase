using WorkBase.Shared.Domain;

namespace WorkBase.Modules.Leave.Domain.Events;

public sealed record LeaveRequestSubmittedEvent(
    Guid LeaveRequestId,
    Guid TenantId,
    Guid EmployeeId,
    Guid LeaveTypeId,
    DateTime StartDate,
    DateTime EndDate,
    decimal TotalDays) : DomainEvent;

public sealed record LeaveRequestApprovedEvent(
    Guid LeaveRequestId,
    Guid TenantId,
    Guid EmployeeId,
    Guid LeaveTypeId,
    decimal TotalDays) : DomainEvent;

public sealed record LeaveRequestRejectedEvent(
    Guid LeaveRequestId,
    Guid TenantId,
    Guid EmployeeId) : DomainEvent;

public sealed record LeaveRequestCancelledEvent(
    Guid LeaveRequestId,
    Guid TenantId,
    Guid EmployeeId,
    decimal TotalDays) : DomainEvent;

public sealed record LeaveBalanceAdjustedEvent(
    Guid BalanceId,
    Guid TenantId,
    Guid EmployeeId,
    Guid LeaveTypeId,
    int Year,
    decimal OldTotal,
    decimal NewTotal) : DomainEvent;

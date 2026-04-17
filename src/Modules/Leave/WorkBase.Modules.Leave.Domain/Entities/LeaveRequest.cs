using WorkBase.Modules.Leave.Domain.Events;
using WorkBase.Shared.Domain;

namespace WorkBase.Modules.Leave.Domain.Entities;

/// <summary>
/// Wniosek urlopowy / o nieobecność.
/// </summary>
public sealed class LeaveRequest : AuditableEntity<Guid>, ITenantScoped, IAuditable
{
    public Guid TenantId { get; private set; }
    public Guid EmployeeId { get; private set; }
    public Guid LeaveTypeId { get; private set; }
    public DateTime StartDate { get; private set; }
    public DateTime EndDate { get; private set; }
    public decimal TotalDays { get; private set; }
    public LeaveRequestStatus Status { get; private set; }
    public string? Reason { get; private set; }
    public Guid? WorkflowInstanceId { get; private set; }
    public string? CustomFields { get; private set; }

    private LeaveRequest() { }

    public static LeaveRequest Create(
        Guid tenantId,
        Guid employeeId,
        Guid leaveTypeId,
        DateTime startDate,
        DateTime endDate,
        decimal totalDays,
        string? reason = null)
    {
        return new LeaveRequest
        {
            TenantId = tenantId,
            EmployeeId = employeeId,
            LeaveTypeId = leaveTypeId,
            StartDate = startDate,
            EndDate = endDate,
            TotalDays = totalDays,
            Status = LeaveRequestStatus.Draft,
            Reason = reason,
        };
    }

    public void Submit()
    {
        Status = LeaveRequestStatus.Pending;
        RaiseDomainEvent(new LeaveRequestSubmittedEvent(
            Id, TenantId, EmployeeId, LeaveTypeId, StartDate, EndDate, TotalDays));
    }

    public void Approve()
    {
        Status = LeaveRequestStatus.Approved;
        RaiseDomainEvent(new LeaveRequestApprovedEvent(
            Id, TenantId, EmployeeId, LeaveTypeId, TotalDays));
    }

    public void Reject()
    {
        Status = LeaveRequestStatus.Rejected;
        RaiseDomainEvent(new LeaveRequestRejectedEvent(Id, TenantId, EmployeeId));
    }

    public void Cancel()
    {
        Status = LeaveRequestStatus.Cancelled;
        RaiseDomainEvent(new LeaveRequestCancelledEvent(
            Id, TenantId, EmployeeId, TotalDays));
    }

    public void Return() => Status = LeaveRequestStatus.Draft;

    public void LinkWorkflow(Guid workflowInstanceId)
    {
        WorkflowInstanceId = workflowInstanceId;
    }
}

public enum LeaveRequestStatus
{
    Draft = 0,
    Pending = 1,
    Approved = 2,
    Rejected = 3,
    Cancelled = 4,
}

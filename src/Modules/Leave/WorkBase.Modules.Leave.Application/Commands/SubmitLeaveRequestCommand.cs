using WorkBase.Shared.Cqrs;

namespace WorkBase.Modules.Leave.Application.Commands;

public sealed record SubmitLeaveRequestCommand(
    Guid EmployeeId,
    Guid LeaveTypeId,
    DateTime StartDate,
    DateTime EndDate,
    decimal TotalDays,
    string? Reason = null) : ICommand<Guid>, ITenantRequest
{
    public Guid TenantId { get; set; }
}

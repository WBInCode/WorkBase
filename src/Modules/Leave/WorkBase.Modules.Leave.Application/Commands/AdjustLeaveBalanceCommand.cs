using WorkBase.Shared.Cqrs;

namespace WorkBase.Modules.Leave.Application.Commands;

public sealed record AdjustLeaveBalanceCommand(
    Guid EmployeeId,
    Guid LeaveTypeId,
    int Year,
    decimal NewTotalDays) : ICommand, ITenantRequest
{
    public Guid TenantId { get; set; }
}

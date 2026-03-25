using WorkBase.Shared.Cqrs;

namespace WorkBase.Modules.Organization.Application.Commands.Employees;

public sealed record SetSupervisorCommand(
    Guid EmployeeId,
    Guid SupervisorEmployeeId) : ICommand, ITenantRequest
{
    public Guid TenantId { get; set; }
}

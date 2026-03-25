using WorkBase.Shared.Cqrs;

namespace WorkBase.Modules.Organization.Application.Commands.Employees;

public sealed record AssignEmployeeCommand(
    Guid EmployeeId,
    Guid OrganizationUnitId,
    Guid PositionId,
    bool IsPrimary,
    DateTime StartDate) : ICommand<Guid>, ITenantRequest
{
    public Guid TenantId { get; set; }
}

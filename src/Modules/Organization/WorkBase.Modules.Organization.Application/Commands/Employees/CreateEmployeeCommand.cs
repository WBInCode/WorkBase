using WorkBase.Shared.Cqrs;

namespace WorkBase.Modules.Organization.Application.Commands.Employees;

public sealed record CreateEmployeeCommand(
    string FirstName,
    string LastName,
    string Email,
    string? EmployeeNumber,
    DateTime HireDate) : ICommand<Guid>, ITenantRequest
{
    public Guid TenantId { get; set; }
}

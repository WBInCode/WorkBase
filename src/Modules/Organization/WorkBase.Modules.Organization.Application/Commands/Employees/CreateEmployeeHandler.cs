using WorkBase.Contracts;
using WorkBase.Modules.Organization.Application.Contracts;
using WorkBase.Modules.Organization.Domain.Entities;
using WorkBase.Shared.Cqrs;
using WorkBase.Shared.Domain;

namespace WorkBase.Modules.Organization.Application.Commands.Employees;

public sealed class CreateEmployeeHandler(
    IEmployeeRepository employeeRepository,
    IEmployeeAccessProvisioningQueue accessProvisioningQueue)
    : ICommandHandler<CreateEmployeeCommand, Guid>
{
    public async Task<Result<Guid>> Handle(
        CreateEmployeeCommand request,
        CancellationToken cancellationToken)
    {
        if (await employeeRepository.EmailExistsInTenantAsync(request.TenantId, request.Email, cancellationToken: cancellationToken))
            return Result.Failure<Guid>(Error.Conflict("Employee.EmailExists", $"Employee with email '{request.Email}' already exists."));

        var employee = Employee.Create(
            request.TenantId,
            request.FirstName,
            request.LastName,
            request.Email,
            request.EmployeeNumber,
            request.HireDate);

        await employeeRepository.AddAsync(employee, cancellationToken);
        await accessProvisioningQueue.QueueInvitationAsync(
            new EmployeeAccessInvitationRequest(
                employee.TenantId,
                employee.Id,
                employee.Email,
                employee.FirstName,
                employee.LastName),
            cancellationToken);

        return employee.Id;
    }
}

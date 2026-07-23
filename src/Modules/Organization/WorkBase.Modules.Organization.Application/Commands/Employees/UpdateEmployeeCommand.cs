using WorkBase.Contracts;
using WorkBase.Modules.Organization.Application.Contracts;
using WorkBase.Shared.Cqrs;
using WorkBase.Shared.Domain;

namespace WorkBase.Modules.Organization.Application.Commands.Employees;

public sealed record UpdateEmployeeCommand(
    Guid Id,
    string FirstName,
    string LastName,
    string Email,
    string? EmployeeNumber) : ICommand, ITenantRequest
{
    public Guid TenantId { get; set; }
}

public sealed class UpdateEmployeeHandler(
    IEmployeeRepository repository,
    IEmployeeAccessStatusService accessStatusService)
    : ICommandHandler<UpdateEmployeeCommand>
{
    public async Task<Result> Handle(UpdateEmployeeCommand request, CancellationToken cancellationToken)
    {
        var employee = await repository.GetByIdAsync(request.Id, cancellationToken);
        if (employee is null || employee.TenantId != request.TenantId)
            return Result.Failure(Error.NotFound("Employee.NotFound", "Pracownik nie został znaleziony."));

        var emailChanged = !string.Equals(
            employee.Email.Trim(), request.Email.Trim(), StringComparison.OrdinalIgnoreCase);
        if (emailChanged)
        {
            var access = await accessStatusService.GetAsync(
                request.TenantId, employee.Id, cancellationToken);
            if (access?.ManagedByHub == true && access.Status is not null and not "NotRequested")
            {
                return Result.Failure(Error.Conflict(
                    "Employee.EmailManagedByHub",
                    "Adres e-mail użytkownika z dostępem do WorkBase jest zarządzany w WB Platform."));
            }
        }

        var emailExists = await repository.EmailExistsInTenantAsync(
            request.TenantId, request.Email, request.Id, cancellationToken);
        if (emailExists)
            return Result.Failure(Error.Conflict("Employee.EmailExists", "Pracownik o podanym adresie email już istnieje."));

        employee.Update(request.FirstName, request.LastName, request.Email, request.EmployeeNumber);
        repository.Update(employee);
        return Result.Success();
    }
}

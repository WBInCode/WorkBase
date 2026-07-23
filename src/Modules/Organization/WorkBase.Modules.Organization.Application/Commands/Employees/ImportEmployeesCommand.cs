using WorkBase.Contracts;
using WorkBase.Modules.Organization.Application.Contracts;
using WorkBase.Modules.Organization.Domain.Entities;
using WorkBase.Shared.Cqrs;
using WorkBase.Shared.Domain;

namespace WorkBase.Modules.Organization.Application.Commands.Employees;

public sealed record ImportEmployeesCommand(
    List<ImportEmployeeRow> Rows) : ICommand<ImportEmployeesResult>, ITenantRequest
{
    public Guid TenantId { get; set; }
}

public sealed record ImportEmployeeRow(
    string FirstName, string LastName, string Email,
    string? EmployeeNumber, DateTime HireDate);

public sealed record ImportEmployeesResult(int Imported, int Skipped, List<string> Errors);

public sealed class ImportEmployeesHandler(
    IEmployeeRepository employeeRepository,
    IEmployeeAccessProvisioningQueue accessProvisioningQueue)
    : ICommandHandler<ImportEmployeesCommand, ImportEmployeesResult>
{
    public async Task<Result<ImportEmployeesResult>> Handle(
        ImportEmployeesCommand request, CancellationToken cancellationToken)
    {
        var imported = 0;
        var skipped = 0;
        var errors = new List<string>();

        foreach (var row in request.Rows)
        {
            if (string.IsNullOrWhiteSpace(row.Email))
            {
                errors.Add($"Wiersz {imported + skipped + 1}: brak adresu email.");
                skipped++;
                continue;
            }

            if (await employeeRepository.EmailExistsInTenantAsync(
                request.TenantId, row.Email, cancellationToken: cancellationToken))
            {
                errors.Add($"Pracownik z emailem '{row.Email}' już istnieje — pominięto.");
                skipped++;
                continue;
            }

            var employee = Employee.Create(
                request.TenantId, row.FirstName, row.LastName,
                row.Email, row.EmployeeNumber, row.HireDate);
            await employeeRepository.AddAsync(employee, cancellationToken);
            await accessProvisioningQueue.QueueInvitationAsync(
                new EmployeeAccessInvitationRequest(
                    employee.TenantId,
                    employee.Id,
                    employee.Email,
                    employee.FirstName,
                    employee.LastName),
                cancellationToken);
            imported++;
        }

        return new ImportEmployeesResult(imported, skipped, errors);
    }
}

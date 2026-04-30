using WorkBase.Modules.Organization.Application.Contracts;
using WorkBase.Modules.Organization.Application.Dtos;
using WorkBase.Shared.Cqrs;
using WorkBase.Shared.Domain;

namespace WorkBase.Modules.Organization.Application.Queries.Employees;

public sealed record GetEmployeeByNumberQuery(Guid TenantId, string EmployeeNumber) : IQuery<EmployeeDto>;

public sealed class GetEmployeeByNumberHandler(IEmployeeRepository employeeRepository)
    : IQueryHandler<GetEmployeeByNumberQuery, EmployeeDto>
{
    public async Task<Result<EmployeeDto>> Handle(
        GetEmployeeByNumberQuery request,
        CancellationToken cancellationToken)
    {
        var employee = await employeeRepository.GetByEmployeeNumberAsync(
            request.TenantId, request.EmployeeNumber, cancellationToken);

        if (employee is null)
            return Result.Failure<EmployeeDto>(Error.NotFound("Employee.NotFound", "Nie znaleziono pracownika o podanym numerze."));

        var primaryUnits = await employeeRepository.GetPrimaryAssignmentsAsync(
            new[] { employee.Id }, cancellationToken);
        var hasUnit = primaryUnits.TryGetValue(employee.Id, out var unit);

        return new EmployeeDto(
            employee.Id,
            employee.FirstName,
            employee.LastName,
            employee.Email,
            employee.EmployeeNumber,
            employee.HireDate,
            employee.TerminationDate,
            employee.Status.ToString(),
            employee.UserId,
            employee.HourlyRate,
            hasUnit ? unit.UnitId : null,
            hasUnit ? unit.UnitName : null);
    }
}

using WorkBase.Modules.Organization.Application.Contracts;
using WorkBase.Modules.Organization.Application.Dtos;
using WorkBase.Shared.Cqrs;
using WorkBase.Shared.Domain;

namespace WorkBase.Modules.Organization.Application.Queries.Employees;

public sealed class GetEmployeesHandler(IEmployeeRepository employeeRepository)
    : IQueryHandler<GetEmployeesQuery, PagedResultDto<EmployeeDto>>
{
    public async Task<Result<PagedResultDto<EmployeeDto>>> Handle(
        GetEmployeesQuery request,
        CancellationToken cancellationToken)
    {
        var (items, totalCount) = await employeeRepository.GetPagedAsync(
            request.TenantId,
            request.Search,
            request.OrganizationUnitId,
            request.Status,
            request.Page,
            request.PageSize,
            cancellationToken);

        var primaryUnits = await employeeRepository.GetPrimaryAssignmentsAsync(
            items.Select(e => e.Id),
            cancellationToken);

        var dtos = items.Select(e =>
        {
            var hasUnit = primaryUnits.TryGetValue(e.Id, out var unit);
            return new EmployeeDto(
                e.Id,
                e.FirstName,
                e.LastName,
                e.Email,
                e.EmployeeNumber,
                e.HireDate,
                e.TerminationDate,
                e.Status.ToString(),
                e.UserId,
                e.HourlyRate,
                hasUnit ? unit.UnitId : null,
                hasUnit ? unit.UnitName : null);
        }).ToList();

        return new PagedResultDto<EmployeeDto>(dtos, totalCount, request.Page, request.PageSize);
    }
}

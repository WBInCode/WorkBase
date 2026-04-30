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

        var dtos = items.Select(e => new EmployeeDto(
            e.Id,
            e.FirstName,
            e.LastName,
            e.Email,
            e.EmployeeNumber,
            e.HireDate,
            e.TerminationDate,
            e.Status.ToString(),
            e.UserId,
            e.HourlyRate)).ToList();

        return new PagedResultDto<EmployeeDto>(dtos, totalCount, request.Page, request.PageSize);
    }
}

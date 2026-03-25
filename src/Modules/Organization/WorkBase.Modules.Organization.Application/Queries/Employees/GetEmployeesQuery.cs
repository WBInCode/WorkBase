using WorkBase.Modules.Organization.Application.Dtos;
using WorkBase.Modules.Organization.Domain.Entities;
using WorkBase.Shared.Cqrs;

namespace WorkBase.Modules.Organization.Application.Queries.Employees;

public sealed record GetEmployeesQuery(
    string? Search,
    Guid? OrganizationUnitId,
    EmployeeStatus? Status,
    int Page = 1,
    int PageSize = 20) : IQuery<PagedResultDto<EmployeeDto>>, ITenantRequest
{
    public Guid TenantId { get; set; }
}

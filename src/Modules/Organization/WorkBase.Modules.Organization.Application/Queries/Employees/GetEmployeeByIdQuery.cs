using WorkBase.Modules.Organization.Application.Dtos;
using WorkBase.Shared.Cqrs;

namespace WorkBase.Modules.Organization.Application.Queries.Employees;

public sealed record GetEmployeeByIdQuery(Guid EmployeeId) : IQuery<EmployeeDetailDto>;

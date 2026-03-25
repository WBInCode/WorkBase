using WorkBase.Modules.Organization.Application.Contracts;
using WorkBase.Modules.Organization.Application.Dtos;
using WorkBase.Shared.Cqrs;
using WorkBase.Shared.Domain;

namespace WorkBase.Modules.Organization.Application.Queries.Employees;

public sealed class GetEmployeeByIdHandler(
    IEmployeeRepository employeeRepository,
    IEmployeeAssignmentRepository assignmentRepository,
    ISupervisorRelationRepository supervisorRepository,
    IOrganizationUnitRepository unitRepository,
    IPositionRepository positionRepository)
    : IQueryHandler<GetEmployeeByIdQuery, EmployeeDetailDto>
{
    public async Task<Result<EmployeeDetailDto>> Handle(
        GetEmployeeByIdQuery request,
        CancellationToken cancellationToken)
    {
        var employee = await employeeRepository.GetByIdAsync(request.EmployeeId, cancellationToken);
        if (employee is null)
            return Result.Failure<EmployeeDetailDto>(Error.NotFound("Employee.NotFound", "Employee not found."));

        var assignments = await assignmentRepository.GetByEmployeeAsync(request.EmployeeId, cancellationToken);

        var assignmentDtos = new List<EmployeeAssignmentDto>();
        foreach (var a in assignments.Where(a => a.EndDate is null))
        {
            var unit = await unitRepository.GetByIdAsync(a.OrganizationUnitId, cancellationToken);
            var position = await positionRepository.GetByIdAsync(a.PositionId, cancellationToken);

            assignmentDtos.Add(new EmployeeAssignmentDto(
                a.Id,
                a.OrganizationUnitId,
                unit?.Name ?? "Unknown",
                a.PositionId,
                position?.Name ?? "Unknown",
                a.IsPrimary,
                a.StartDate,
                a.EndDate));
        }

        SupervisorInfoDto? supervisorInfo = null;
        var supervisorRelation = await supervisorRepository.GetActiveBySubordinateAsync(request.EmployeeId, cancellationToken);
        if (supervisorRelation is not null)
        {
            var supervisor = await employeeRepository.GetByIdAsync(supervisorRelation.SupervisorEmployeeId, cancellationToken);
            if (supervisor is not null)
            {
                supervisorInfo = new SupervisorInfoDto(
                    supervisor.Id,
                    supervisor.FirstName,
                    supervisor.LastName);
            }
        }

        var dto = new EmployeeDetailDto(
            employee.Id,
            employee.FirstName,
            employee.LastName,
            employee.Email,
            employee.EmployeeNumber,
            employee.HireDate,
            employee.TerminationDate,
            employee.Status.ToString(),
            employee.UserId,
            assignmentDtos,
            supervisorInfo);

        return dto;
    }
}

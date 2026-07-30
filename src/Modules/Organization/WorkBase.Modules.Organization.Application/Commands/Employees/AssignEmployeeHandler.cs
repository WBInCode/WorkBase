using WorkBase.Modules.Organization.Application.Contracts;
using WorkBase.Modules.Organization.Application.Services;
using WorkBase.Modules.Organization.Domain.Entities;
using WorkBase.Modules.Organization.Domain.Events;
using WorkBase.Shared.Cqrs;
using WorkBase.Shared.Domain;

namespace WorkBase.Modules.Organization.Application.Commands.Employees;

public sealed class AssignEmployeeHandler(
    IEmployeeRepository employeeRepository,
    IOrganizationUnitRepository unitRepository,
    IPositionRepository positionRepository,
    IEmployeeAssignmentRepository assignmentRepository,
    PositionAssignmentPolicy positionPolicy)
    : ICommandHandler<AssignEmployeeCommand, Guid>
{
    public async Task<Result<Guid>> Handle(
        AssignEmployeeCommand request,
        CancellationToken cancellationToken)
    {
        var employee = await employeeRepository.GetByIdAsync(request.EmployeeId, cancellationToken);
        if (employee is null)
            return Result.Failure<Guid>(Error.NotFound("Employee.NotFound", "Employee not found."));

        if (!await unitRepository.ExistsAsync(request.OrganizationUnitId, cancellationToken))
            return Result.Failure<Guid>(Error.NotFound("Unit.NotFound", "Organization unit not found."));

        var position = await positionRepository.GetByIdAsync(request.PositionId, cancellationToken);
        if (position is null)
            return Result.Failure<Guid>(Error.NotFound("Position.NotFound", "Position not found."));

        if (request.IsPrimary)
        {
            var currentPrimary = await assignmentRepository.GetPrimaryByEmployeeAsync(request.EmployeeId, cancellationToken);
            if (currentPrimary is not null)
            {
                currentPrimary.End(request.StartDate);
                assignmentRepository.Update(currentPrimary);
            }
        }

        var assignment = EmployeeAssignment.Create(
            request.TenantId,
            request.EmployeeId,
            request.OrganizationUnitId,
            request.PositionId,
            request.IsPrimary,
            request.StartDate);

        await assignmentRepository.AddAsync(assignment, cancellationToken);

        await positionPolicy.ApplyAsync(
            employee, request.OrganizationUnitId, position, request.TenantId, cancellationToken);

        employee.RaiseDomainEvent(new EmployeeAssignmentChangedEvent(
            request.EmployeeId,
            request.OrganizationUnitId,
            request.PositionId,
            request.TenantId));

        employeeRepository.Update(employee);

        return assignment.Id;
    }
}

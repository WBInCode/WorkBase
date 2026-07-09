using WorkBase.Modules.Organization.Application.Contracts;
using WorkBase.Modules.Organization.Domain.Entities;
using WorkBase.Modules.Organization.Domain.Events;
using WorkBase.Shared.Cqrs;
using WorkBase.Shared.Domain;

namespace WorkBase.Modules.Organization.Application.Commands.Employees;

public sealed class SetSupervisorHandler(
    IEmployeeRepository employeeRepository,
    ISupervisorRelationRepository supervisorRepository)
    : ICommandHandler<SetSupervisorCommand>
{
    public async Task<Result> Handle(
        SetSupervisorCommand request,
        CancellationToken cancellationToken)
    {
        if (request.EmployeeId == request.SupervisorEmployeeId)
            return Result.Failure(Error.Validation("Supervisor.SelfReference", "Employee cannot be their own supervisor."));

        var employee = await employeeRepository.GetByIdAsync(request.EmployeeId, cancellationToken);
        if (employee is null)
            return Result.Failure(Error.NotFound("Employee.NotFound", "Employee not found."));

        if (!await employeeRepository.ExistsAsync(request.SupervisorEmployeeId, cancellationToken))
            return Result.Failure(Error.NotFound("Supervisor.NotFound", "Supervisor employee not found."));

        // Walk up the proposed supervisor's own chain of supervisors: if it ever reaches back
        // to the employee being assigned, setting this supervisor would create a cycle
        // (e.g. A supervises B supervises A), which would break any code that walks the chain
        // (org chart rendering, future multi-level escalation) — see
        // docs/AUDIT-KNOWLEDGE-MAP.md (team/role/employee consistency).
        var currentId = request.SupervisorEmployeeId;
        for (var depth = 0; depth < 50; depth++)
        {
            if (currentId == request.EmployeeId)
                return Result.Failure(Error.Validation("Supervisor.CircularReference",
                    "Ta zmiana utworzyłaby cykl w hierarchii przełożonych."));

            var ancestorRelation = await supervisorRepository.GetActiveBySubordinateAsync(currentId, cancellationToken);
            if (ancestorRelation is null) break;
            currentId = ancestorRelation.SupervisorEmployeeId;
        }

        var currentRelation = await supervisorRepository.GetActiveBySubordinateAsync(request.EmployeeId, cancellationToken);
        if (currentRelation is not null)
        {
            currentRelation.End(DateTime.UtcNow);
            supervisorRepository.Update(currentRelation);
        }

        var relation = SupervisorRelation.Create(
            request.TenantId,
            request.SupervisorEmployeeId,
            request.EmployeeId,
            DateTime.UtcNow);

        await supervisorRepository.AddAsync(relation, cancellationToken);

        employee.RaiseDomainEvent(new SupervisorChangedEvent(
            request.EmployeeId,
            request.SupervisorEmployeeId,
            request.TenantId));

        employeeRepository.Update(employee);

        return Result.Success();
    }
}

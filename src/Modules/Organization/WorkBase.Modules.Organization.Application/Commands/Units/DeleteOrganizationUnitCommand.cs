using WorkBase.Modules.Organization.Application.Contracts;
using WorkBase.Shared.Cqrs;
using WorkBase.Shared.Domain;

namespace WorkBase.Modules.Organization.Application.Commands.Units;

public sealed record DeleteOrganizationUnitCommand(Guid Id) : ICommand, ITenantRequest
{
    public Guid TenantId { get; set; }
}

public sealed class DeleteOrganizationUnitHandler(
    IOrganizationUnitRepository repository,
    IEmployeeAssignmentRepository assignmentRepository)
    : ICommandHandler<DeleteOrganizationUnitCommand>
{
    public async Task<Result> Handle(DeleteOrganizationUnitCommand request, CancellationToken cancellationToken)
    {
        var unit = await repository.GetByIdAsync(request.Id, cancellationToken);
        if (unit is null || unit.TenantId != request.TenantId)
            return Result.Failure(Error.NotFound("Unit.NotFound", "Jednostka organizacyjna nie została znaleziona."));

        // Recursively delete children first (depth-first)
        await DeleteRecursiveAsync(request.Id, cancellationToken);

        return Result.Success();
    }

    private async Task DeleteRecursiveAsync(Guid unitId, CancellationToken cancellationToken)
    {
        var children = await repository.GetChildrenAsync(unitId, cancellationToken);
        foreach (var child in children)
        {
            await DeleteRecursiveAsync(child.Id, cancellationToken);
        }

        // Remove employee assignments for this unit
        var assignments = await assignmentRepository.GetByOrgUnitAsync(unitId, cancellationToken);
        if (assignments.Count > 0)
            assignmentRepository.RemoveRange(assignments);

        // Remove the unit itself (closure entries cascade-delete automatically)
        var unit = await repository.GetByIdAsync(unitId, cancellationToken);
        if (unit is not null)
            repository.Remove(unit);
    }
}

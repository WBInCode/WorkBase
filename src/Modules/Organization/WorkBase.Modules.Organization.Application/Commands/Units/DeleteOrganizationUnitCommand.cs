using WorkBase.Modules.Organization.Application.Contracts;
using WorkBase.Shared.Cqrs;
using WorkBase.Shared.Domain;

namespace WorkBase.Modules.Organization.Application.Commands.Units;

public sealed record DeleteOrganizationUnitCommand(Guid Id) : ICommand, ITenantRequest
{
    public Guid TenantId { get; set; }
}

public sealed class DeleteOrganizationUnitHandler(IOrganizationUnitRepository repository)
    : ICommandHandler<DeleteOrganizationUnitCommand>
{
    public async Task<Result> Handle(DeleteOrganizationUnitCommand request, CancellationToken cancellationToken)
    {
        var unit = await repository.GetByIdAsync(request.Id, cancellationToken);
        if (unit is null || unit.TenantId != request.TenantId)
            return Result.Failure(Error.NotFound("Unit.NotFound", "Jednostka organizacyjna nie została znaleziona."));

        repository.Remove(unit);
        return Result.Success();
    }
}

using WorkBase.Modules.Organization.Application.Contracts;
using WorkBase.Shared.Cqrs;
using WorkBase.Shared.Domain;

namespace WorkBase.Modules.Organization.Application.Commands.UnitTypes;

public sealed record DeleteUnitTypeCommand(Guid Id) : ICommand, ITenantRequest
{
    public Guid TenantId { get; set; }
}

public sealed class DeleteUnitTypeHandler(IOrganizationUnitTypeRepository repository)
    : ICommandHandler<DeleteUnitTypeCommand>
{
    public async Task<Result> Handle(DeleteUnitTypeCommand request, CancellationToken cancellationToken)
    {
        var unitType = await repository.GetByIdAsync(request.Id, cancellationToken);
        if (unitType is null || unitType.TenantId != request.TenantId)
            return Result.Failure(Error.NotFound("UnitType.NotFound", "Typ jednostki nie został znaleziony."));

        repository.Remove(unitType);
        return Result.Success();
    }
}

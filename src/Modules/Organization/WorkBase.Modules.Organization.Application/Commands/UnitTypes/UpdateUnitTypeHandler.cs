using WorkBase.Modules.Organization.Application.Contracts;
using WorkBase.Shared.Cqrs;
using WorkBase.Shared.Domain;

namespace WorkBase.Modules.Organization.Application.Commands.UnitTypes;

public sealed class UpdateUnitTypeHandler(IOrganizationUnitTypeRepository unitTypeRepository)
    : ICommandHandler<UpdateUnitTypeCommand>
{
    public async Task<Result> Handle(
        UpdateUnitTypeCommand request,
        CancellationToken cancellationToken)
    {
        var unitType = await unitTypeRepository.GetByIdAsync(request.UnitTypeId, cancellationToken);

        if (unitType is null)
            return Result.Failure(Error.NotFound("UnitType.NotFound", $"Unit type '{request.UnitTypeId}' not found."));

        if (await unitTypeRepository.NameExistsInTenantAsync(request.TenantId, request.Name, request.UnitTypeId, cancellationToken))
            return Result.Failure(Error.Conflict("UnitType.NameExists", $"Unit type '{request.Name}' already exists."));

        unitType.Update(request.Name, request.Description, request.SortOrder);
        unitTypeRepository.Update(unitType);

        return Result.Success();
    }
}

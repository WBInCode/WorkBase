using WorkBase.Modules.Organization.Application.Contracts;
using WorkBase.Shared.Cqrs;
using WorkBase.Shared.Domain;

namespace WorkBase.Modules.Organization.Application.Commands.Units;

public sealed class UpdateOrganizationUnitHandler(
    IOrganizationUnitRepository unitRepository,
    IOrganizationUnitTypeRepository unitTypeRepository)
    : ICommandHandler<UpdateOrganizationUnitCommand>
{
    public async Task<Result> Handle(
        UpdateOrganizationUnitCommand request,
        CancellationToken cancellationToken)
    {
        var unit = await unitRepository.GetByIdAsync(request.UnitId, cancellationToken);
        if (unit is null)
            return Result.Failure(Error.NotFound("Unit.NotFound", "Organization unit not found."));

        if (!await unitTypeRepository.ExistsAsync(request.TypeId, cancellationToken))
            return Result.Failure(Error.NotFound("UnitType.NotFound", "Organization unit type not found."));

        if (request.Code is not null &&
            await unitRepository.CodeExistsInTenantAsync(request.TenantId, request.Code, request.UnitId, cancellationToken))
            return Result.Failure(Error.Conflict("Unit.CodeExists", $"Organization unit with code '{request.Code}' already exists."));

        unit.Update(request.Name, request.Code, request.TypeId);
        unitRepository.Update(unit);

        return Result.Success();
    }
}

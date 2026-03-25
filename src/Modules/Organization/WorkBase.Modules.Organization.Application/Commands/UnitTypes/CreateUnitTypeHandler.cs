using WorkBase.Modules.Organization.Application.Contracts;
using WorkBase.Modules.Organization.Domain.Entities;
using WorkBase.Shared.Cqrs;
using WorkBase.Shared.Domain;

namespace WorkBase.Modules.Organization.Application.Commands.UnitTypes;

public sealed class CreateUnitTypeHandler(IOrganizationUnitTypeRepository unitTypeRepository)
    : ICommandHandler<CreateUnitTypeCommand, Guid>
{
    public async Task<Result<Guid>> Handle(
        CreateUnitTypeCommand request,
        CancellationToken cancellationToken)
    {
        if (await unitTypeRepository.NameExistsInTenantAsync(request.TenantId, request.Name, cancellationToken: cancellationToken))
            return Result.Failure<Guid>(Error.Conflict("UnitType.NameExists", $"Unit type '{request.Name}' already exists."));

        var unitType = OrganizationUnitType.Create(
            request.TenantId,
            request.Name,
            request.Description,
            request.SortOrder);

        await unitTypeRepository.AddAsync(unitType, cancellationToken);

        return unitType.Id;
    }
}

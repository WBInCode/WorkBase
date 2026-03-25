using WorkBase.Modules.Organization.Application.Contracts;
using WorkBase.Modules.Organization.Domain.Entities;
using WorkBase.Shared.Cqrs;
using WorkBase.Shared.Domain;

namespace WorkBase.Modules.Organization.Application.Commands.Units;

public sealed class CreateOrganizationUnitHandler(
    IOrganizationUnitRepository unitRepository,
    IOrganizationUnitTypeRepository unitTypeRepository)
    : ICommandHandler<CreateOrganizationUnitCommand, Guid>
{
    public async Task<Result<Guid>> Handle(
        CreateOrganizationUnitCommand request,
        CancellationToken cancellationToken)
    {
        if (!await unitTypeRepository.ExistsAsync(request.TypeId, cancellationToken))
            return Result.Failure<Guid>(Error.NotFound("UnitType.NotFound", "Organization unit type not found."));

        if (request.ParentId.HasValue &&
            !await unitRepository.ExistsAsync(request.ParentId.Value, cancellationToken))
            return Result.Failure<Guid>(Error.NotFound("Unit.ParentNotFound", "Parent organization unit not found."));

        if (request.Code is not null &&
            await unitRepository.CodeExistsInTenantAsync(request.TenantId, request.Code, cancellationToken: cancellationToken))
            return Result.Failure<Guid>(Error.Conflict("Unit.CodeExists", $"Organization unit with code '{request.Code}' already exists."));

        var unit = OrganizationUnit.Create(
            request.TenantId,
            request.Name,
            request.Code,
            request.TypeId,
            request.ParentId);

        await unitRepository.AddAsync(unit, cancellationToken);
        await unitRepository.InsertClosureForNewUnitAsync(unit.Id, request.ParentId, cancellationToken);

        return unit.Id;
    }
}

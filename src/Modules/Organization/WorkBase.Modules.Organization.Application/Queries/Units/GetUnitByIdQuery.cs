using WorkBase.Modules.Organization.Application.Contracts;
using WorkBase.Modules.Organization.Application.Dtos;
using WorkBase.Shared.Cqrs;
using WorkBase.Shared.Domain;

namespace WorkBase.Modules.Organization.Application.Queries.Units;

public sealed record GetUnitByIdQuery(Guid Id) : IQuery<OrganizationUnitDto>, ITenantRequest
{
    public Guid TenantId { get; set; }
}

public sealed class GetUnitByIdHandler(
    IOrganizationUnitRepository unitRepository,
    IOrganizationUnitTypeRepository typeRepository)
    : IQueryHandler<GetUnitByIdQuery, OrganizationUnitDto>
{
    public async Task<Result<OrganizationUnitDto>> Handle(GetUnitByIdQuery request, CancellationToken cancellationToken)
    {
        var unit = await unitRepository.GetByIdAsync(request.Id, cancellationToken);
        if (unit is null || unit.TenantId != request.TenantId)
            return Result.Failure<OrganizationUnitDto>(Error.NotFound("Unit.NotFound", "Jednostka organizacyjna nie została znaleziona."));

        var type = await typeRepository.GetByIdAsync(unit.TypeId, cancellationToken);

        return new OrganizationUnitDto(
            unit.Id,
            unit.Name,
            unit.Code,
            unit.TypeId,
            type?.Name ?? "?",
            unit.ParentId,
            unit.IsActive);
    }
}

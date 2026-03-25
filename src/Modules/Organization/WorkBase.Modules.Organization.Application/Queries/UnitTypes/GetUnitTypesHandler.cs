using WorkBase.Modules.Organization.Application.Contracts;
using WorkBase.Modules.Organization.Application.Dtos;
using WorkBase.Shared.Cqrs;
using WorkBase.Shared.Domain;

namespace WorkBase.Modules.Organization.Application.Queries.UnitTypes;

public sealed class GetUnitTypesHandler(IOrganizationUnitTypeRepository unitTypeRepository)
    : IQueryHandler<GetUnitTypesQuery, List<OrganizationUnitTypeDto>>
{
    public async Task<Result<List<OrganizationUnitTypeDto>>> Handle(
        GetUnitTypesQuery request,
        CancellationToken cancellationToken)
    {
        var types = await unitTypeRepository.GetAllByTenantAsync(request.TenantId, cancellationToken);

        var dtos = types
            .Select(t => new OrganizationUnitTypeDto(t.Id, t.Name, t.Description, t.SortOrder, t.IsActive))
            .ToList();

        return dtos;
    }
}

using WorkBase.Modules.Organization.Application.Dtos;
using WorkBase.Shared.Cqrs;

namespace WorkBase.Modules.Organization.Application.Queries.UnitTypes;

public sealed record GetUnitTypesQuery : IQuery<List<OrganizationUnitTypeDto>>, ITenantRequest
{
    public Guid TenantId { get; set; }
}

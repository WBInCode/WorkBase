using WorkBase.Modules.Organization.Application.Dtos;
using WorkBase.Shared.Cqrs;

namespace WorkBase.Modules.Organization.Application.Queries.Units;

public sealed record GetUnitTreeQuery : IQuery<List<OrganizationUnitTreeNodeDto>>, ITenantRequest
{
    public Guid TenantId { get; set; }
}

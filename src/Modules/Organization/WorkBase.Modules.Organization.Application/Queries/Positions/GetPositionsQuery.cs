using WorkBase.Modules.Organization.Application.Dtos;
using WorkBase.Shared.Cqrs;

namespace WorkBase.Modules.Organization.Application.Queries.Positions;

public sealed record GetPositionsQuery : IQuery<List<PositionDto>>, ITenantRequest
{
    public Guid TenantId { get; set; }
}

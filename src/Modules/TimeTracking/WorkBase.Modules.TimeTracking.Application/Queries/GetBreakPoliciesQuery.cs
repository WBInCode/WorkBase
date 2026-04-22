using WorkBase.Modules.TimeTracking.Application.Dtos;
using WorkBase.Shared.Cqrs;

namespace WorkBase.Modules.TimeTracking.Application.Queries;

public sealed record GetBreakPoliciesQuery : IQuery<List<BreakPolicyDto>>, ITenantRequest
{
    public Guid TenantId { get; set; }
}

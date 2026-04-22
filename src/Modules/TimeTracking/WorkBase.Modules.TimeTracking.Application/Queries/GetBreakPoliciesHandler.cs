using WorkBase.Modules.TimeTracking.Application.Contracts;
using WorkBase.Modules.TimeTracking.Application.Dtos;
using WorkBase.Shared.Cqrs;
using WorkBase.Shared.Domain;

namespace WorkBase.Modules.TimeTracking.Application.Queries;

public sealed class GetBreakPoliciesHandler(IBreakPolicyRepository breakPolicyRepository)
    : IQueryHandler<GetBreakPoliciesQuery, List<BreakPolicyDto>>
{
    public async Task<Result<List<BreakPolicyDto>>> Handle(
        GetBreakPoliciesQuery request, CancellationToken cancellationToken)
    {
        var policies = await breakPolicyRepository.GetByTenantAsync(request.TenantId, cancellationToken);

        var dtos = policies.Select(p => new BreakPolicyDto(
            p.Id,
            p.Name,
            p.BreakType.ToString(),
            p.MaxPerDay,
            p.MaxMinutesPerBreak,
            p.MaxMinutesPerDay,
            p.IsActive)).ToList();

        return dtos;
    }
}

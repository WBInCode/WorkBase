using WorkBase.Modules.Leave.Application.Contracts;
using WorkBase.Shared.Cqrs;
using WorkBase.Shared.Domain;

namespace WorkBase.Modules.Leave.Application.Queries;

public sealed record LeavePolicyDto(
    Guid Id, Guid LeaveTypeId, string Name, int DaysPerYear, bool AllowCarryOver,
    int MaxCarryOverDays, int? MaxConsecutiveDays, int? MinNoticeDays, bool IsActive);

public sealed record GetLeavePoliciesQuery() : IQuery<List<LeavePolicyDto>>, ITenantRequest
{
    public Guid TenantId { get; set; }
}

public sealed class GetLeavePoliciesHandler(ILeavePolicyRepository repository)
    : IQueryHandler<GetLeavePoliciesQuery, List<LeavePolicyDto>>
{
    public async Task<Result<List<LeavePolicyDto>>> Handle(GetLeavePoliciesQuery request, CancellationToken cancellationToken)
    {
        var policies = await repository.GetByTenantAsync(request.TenantId, cancellationToken);
        return policies.Select(p => new LeavePolicyDto(
            p.Id, p.LeaveTypeId, p.Name, p.DaysPerYear, p.AllowCarryOver,
            p.MaxCarryOverDays, p.MaxConsecutiveDays, p.MinNoticeDays, p.IsActive)).ToList();
    }
}

using WorkBase.Modules.Workflow.Application.Contracts;
using WorkBase.Shared.Cqrs;
using WorkBase.Shared.Domain;

namespace WorkBase.Modules.Workflow.Application.Queries;

public sealed record EscalationRuleDto(
    Guid Id, Guid DefinitionId, string StepName,
    int TimeoutMinutes, string ActionType,
    string? ActionPayloadJson, bool IsActive);

public sealed record GetEscalationRulesQuery(Guid? DefinitionId = null)
    : IQuery<List<EscalationRuleDto>>, ITenantRequest
{
    public Guid TenantId { get; set; }
}

public sealed class GetEscalationRulesHandler(IEscalationRuleRepository repository)
    : IQueryHandler<GetEscalationRulesQuery, List<EscalationRuleDto>>
{
    public async Task<Result<List<EscalationRuleDto>>> Handle(
        GetEscalationRulesQuery request, CancellationToken cancellationToken)
    {
        var rules = request.DefinitionId.HasValue
            ? await repository.GetByDefinitionAsync(request.TenantId, request.DefinitionId.Value, cancellationToken)
            : await repository.GetByTenantAsync(request.TenantId, cancellationToken);

        return rules.Select(r => new EscalationRuleDto(
            r.Id, r.DefinitionId, r.StepName,
            r.TimeoutMinutes, r.ActionType,
            r.ActionPayloadJson, r.IsActive)).ToList();
    }
}

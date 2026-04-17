using WorkBase.Modules.Workflow.Application.Contracts;
using WorkBase.Modules.Workflow.Domain.Entities;
using WorkBase.Shared.Cqrs;
using WorkBase.Shared.Domain;

namespace WorkBase.Modules.Workflow.Application.Commands;

public sealed record CreateEscalationRuleCommand(
    Guid DefinitionId, string StepName, int TimeoutMinutes,
    string ActionType, string? ActionPayloadJson = null) : ICommand<Guid>, ITenantRequest
{
    public Guid TenantId { get; set; }
}

public sealed class CreateEscalationRuleHandler(IEscalationRuleRepository repository)
    : ICommandHandler<CreateEscalationRuleCommand, Guid>
{
    public async Task<Result<Guid>> Handle(CreateEscalationRuleCommand request, CancellationToken cancellationToken)
    {
        var rule = EscalationRule.Create(
            request.TenantId, request.DefinitionId, request.StepName,
            request.TimeoutMinutes, request.ActionType, request.ActionPayloadJson);
        await repository.AddAsync(rule, cancellationToken);
        return rule.Id;
    }
}

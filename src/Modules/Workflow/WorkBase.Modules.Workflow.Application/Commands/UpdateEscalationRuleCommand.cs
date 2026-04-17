using WorkBase.Modules.Workflow.Application.Contracts;
using WorkBase.Shared.Cqrs;
using WorkBase.Shared.Domain;

namespace WorkBase.Modules.Workflow.Application.Commands;

public sealed record UpdateEscalationRuleCommand(
    Guid Id, int TimeoutMinutes, string ActionType,
    string? ActionPayloadJson = null) : ICommand, ITenantRequest
{
    public Guid TenantId { get; set; }
}

public sealed class UpdateEscalationRuleHandler(IEscalationRuleRepository repository)
    : ICommandHandler<UpdateEscalationRuleCommand>
{
    public async Task<Result> Handle(UpdateEscalationRuleCommand request, CancellationToken cancellationToken)
    {
        var rule = await repository.GetByIdAsync(request.Id, cancellationToken);
        if (rule is null)
            return Result.Failure(Error.NotFound("EscalationRule.NotFound",
                $"Reguła eskalacji o id '{request.Id}' nie została znaleziona."));

        rule.Update(request.TimeoutMinutes, request.ActionType, request.ActionPayloadJson);
        return Result.Success();
    }
}

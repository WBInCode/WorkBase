using WorkBase.Modules.Workflow.Application.Contracts;
using WorkBase.Shared.Cqrs;
using WorkBase.Shared.Domain;

namespace WorkBase.Modules.Workflow.Application.Commands;

public sealed record DeleteEscalationRuleCommand(Guid Id) : ICommand, ITenantRequest
{
    public Guid TenantId { get; set; }
}

public sealed class DeleteEscalationRuleHandler(IEscalationRuleRepository repository)
    : ICommandHandler<DeleteEscalationRuleCommand>
{
    public async Task<Result> Handle(DeleteEscalationRuleCommand request, CancellationToken cancellationToken)
    {
        var rule = await repository.GetByIdAsync(request.Id, cancellationToken);
        if (rule is null)
            return Result.Failure(Error.NotFound("EscalationRule.NotFound",
                $"Reguła eskalacji o id '{request.Id}' nie została znaleziona."));

        repository.Remove(rule);
        return Result.Success();
    }
}

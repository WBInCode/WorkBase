using WorkBase.Modules.Workflow.Application.Contracts;
using WorkBase.Shared.Cqrs;
using WorkBase.Shared.Domain;

namespace WorkBase.Modules.Workflow.Application.Commands;

public sealed record DeleteWorkflowDefinitionCommand(Guid Id) : ICommand, ITenantRequest
{
    public Guid TenantId { get; set; }
}

public sealed class DeleteWorkflowDefinitionHandler(IWorkflowDefinitionRepository repository)
    : ICommandHandler<DeleteWorkflowDefinitionCommand>
{
    public async Task<Result> Handle(DeleteWorkflowDefinitionCommand request, CancellationToken cancellationToken)
    {
        var def = await repository.GetByIdAsync(request.Id, cancellationToken);
        if (def is null || def.TenantId != request.TenantId)
            return Result.Failure(Error.NotFound("WorkflowDefinition.NotFound", "Definicja workflow nie została znaleziona."));

        repository.Remove(def);
        return Result.Success();
    }
}

using WorkBase.Modules.Workflow.Application.Contracts;
using WorkBase.Modules.Workflow.Application.Services;
using WorkBase.Shared.Cqrs;
using WorkBase.Shared.Domain;

namespace WorkBase.Modules.Workflow.Application.Commands;

public sealed class UpdateWorkflowDefinitionHandler(
    IWorkflowDefinitionRepository definitionRepository)
    : ICommandHandler<UpdateWorkflowDefinitionCommand>
{
    public async Task<Result> Handle(UpdateWorkflowDefinitionCommand request, CancellationToken cancellationToken)
    {
        var definition = await definitionRepository.GetByIdAsync(request.DefinitionId, cancellationToken);
        if (definition is null)
            return Result.Failure(Error.NotFound("Workflow.DefinitionNotFound",
                $"Definicja workflow o id '{request.DefinitionId}' nie została znaleziona."));

        var parseResult = WorkflowDefinitionParser.Parse(request.DefinitionJson);
        if (parseResult.IsFailure)
            return Result.Failure(parseResult.Error);

        var existing = await definitionRepository.GetByNameAsync(request.TenantId, request.Name, cancellationToken);
        if (existing is not null && existing.Id != request.DefinitionId)
            return Result.Failure(Error.Conflict("Workflow.DefinitionNameExists",
                $"Definicja workflow o nazwie '{request.Name}' już istnieje."));

        definition.Update(request.Name, request.DefinitionJson, request.Description);
        definitionRepository.Update(definition);

        return Result.Success();
    }
}

using WorkBase.Modules.Workflow.Application.Contracts;
using WorkBase.Modules.Workflow.Application.Services;
using WorkBase.Modules.Workflow.Domain.Entities;
using WorkBase.Shared.Cqrs;
using WorkBase.Shared.Domain;

namespace WorkBase.Modules.Workflow.Application.Commands;

public sealed class CreateWorkflowDefinitionHandler(
    IWorkflowDefinitionRepository definitionRepository)
    : ICommandHandler<CreateWorkflowDefinitionCommand, Guid>
{
    public async Task<Result<Guid>> Handle(CreateWorkflowDefinitionCommand request, CancellationToken cancellationToken)
    {
        var parseResult = WorkflowDefinitionParser.Parse(request.DefinitionJson);
        if (parseResult.IsFailure)
            return Result.Failure<Guid>(parseResult.Error);

        var existing = await definitionRepository.GetByNameAsync(request.TenantId, request.Name, cancellationToken);
        if (existing is not null)
            return Result.Failure<Guid>(Error.Conflict("Workflow.DefinitionNameExists",
                $"Definicja workflow o nazwie '{request.Name}' już istnieje."));

        var definition = WorkflowDefinition.Create(
            request.TenantId,
            request.Name,
            request.DefinitionJson,
            request.Description);

        await definitionRepository.AddAsync(definition, cancellationToken);

        return definition.Id;
    }
}

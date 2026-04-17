using WorkBase.Modules.Workflow.Application.Contracts;
using WorkBase.Shared.Cqrs;
using WorkBase.Shared.Domain;

namespace WorkBase.Modules.Workflow.Application.Queries;

public sealed record WorkflowDefinitionVersionDto(
    Guid Id, string Name, int Version, bool IsActive,
    DateTime CreatedAt, DateTime? ModifiedAt);

public sealed record GetWorkflowDefinitionVersionsQuery(Guid DefinitionId)
    : IQuery<List<WorkflowDefinitionVersionDto>>, ITenantRequest
{
    public Guid TenantId { get; set; }
}

public sealed class GetWorkflowDefinitionVersionsHandler(IWorkflowDefinitionRepository repository)
    : IQueryHandler<GetWorkflowDefinitionVersionsQuery, List<WorkflowDefinitionVersionDto>>
{
    public async Task<Result<List<WorkflowDefinitionVersionDto>>> Handle(
        GetWorkflowDefinitionVersionsQuery request, CancellationToken cancellationToken)
    {
        var definition = await repository.GetByIdAsync(request.DefinitionId, cancellationToken);
        if (definition is null)
            return Result.Failure<List<WorkflowDefinitionVersionDto>>(
                Error.NotFound("WorkflowDefinition.NotFound",
                    $"Definicja workflow o id '{request.DefinitionId}' nie została znaleziona."));

        // Return current version info (full version history requires event sourcing)
        return new List<WorkflowDefinitionVersionDto>
        {
            new(definition.Id, definition.Name, definition.Version,
                definition.IsActive, definition.CreatedAt, definition.ModifiedAt)
        };
    }
}

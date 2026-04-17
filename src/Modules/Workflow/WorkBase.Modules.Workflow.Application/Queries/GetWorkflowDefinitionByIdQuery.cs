using WorkBase.Modules.Workflow.Application.Contracts;
using WorkBase.Modules.Workflow.Application.Dtos;
using WorkBase.Shared.Cqrs;
using WorkBase.Shared.Domain;

namespace WorkBase.Modules.Workflow.Application.Queries;

public sealed record GetWorkflowDefinitionByIdQuery(Guid Id) : IQuery<WorkflowDefinitionDto>, ITenantRequest
{
    public Guid TenantId { get; set; }
}

public sealed class GetWorkflowDefinitionByIdHandler(IWorkflowDefinitionRepository repository)
    : IQueryHandler<GetWorkflowDefinitionByIdQuery, WorkflowDefinitionDto>
{
    public async Task<Result<WorkflowDefinitionDto>> Handle(GetWorkflowDefinitionByIdQuery request, CancellationToken cancellationToken)
    {
        var def = await repository.GetByIdAsync(request.Id, cancellationToken);
        if (def is null || def.TenantId != request.TenantId)
            return Result.Failure<WorkflowDefinitionDto>(Error.NotFound("WorkflowDefinition.NotFound", "Definicja workflow nie została znaleziona."));

        return new WorkflowDefinitionDto(
            def.Id, def.Name, def.Description, def.DefinitionJson,
            def.Version, def.IsActive, def.CreatedAt);
    }
}

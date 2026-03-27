using WorkBase.Modules.Workflow.Application.Contracts;
using WorkBase.Modules.Workflow.Application.Dtos;
using WorkBase.Shared.Cqrs;
using WorkBase.Shared.Domain;

namespace WorkBase.Modules.Workflow.Application.Queries;

public sealed class GetWorkflowDefinitionsHandler(
    IWorkflowDefinitionRepository definitionRepository)
    : IQueryHandler<GetWorkflowDefinitionsQuery, List<WorkflowDefinitionDto>>
{
    public async Task<Result<List<WorkflowDefinitionDto>>> Handle(
        GetWorkflowDefinitionsQuery request, CancellationToken cancellationToken)
    {
        var definitions = await definitionRepository.GetAllAsync(request.TenantId, cancellationToken);

        var dtos = definitions.Select(d => new WorkflowDefinitionDto(
            d.Id,
            d.Name,
            d.Description,
            d.DefinitionJson,
            d.Version,
            d.IsActive,
            d.CreatedAt)).ToList();

        return dtos;
    }
}

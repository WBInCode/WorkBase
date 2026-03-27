using WorkBase.Modules.Workflow.Application.Contracts;
using WorkBase.Modules.Workflow.Application.Dtos;
using WorkBase.Shared.Cqrs;
using WorkBase.Shared.Domain;

namespace WorkBase.Modules.Workflow.Application.Queries;

public sealed class GetWorkflowInstanceHandler(
    IWorkflowInstanceRepository instanceRepository,
    IWorkflowDefinitionRepository definitionRepository)
    : IQueryHandler<GetWorkflowInstanceQuery, WorkflowInstanceDto>
{
    public async Task<Result<WorkflowInstanceDto>> Handle(
        GetWorkflowInstanceQuery request, CancellationToken cancellationToken)
    {
        var instance = await instanceRepository.GetByIdAsync(request.InstanceId, cancellationToken);
        if (instance is null)
            return Result.Failure<WorkflowInstanceDto>(Error.NotFound("Workflow.InstanceNotFound",
                $"Instancja workflow o id '{request.InstanceId}' nie została znaleziona."));

        var definition = await definitionRepository.GetByIdAsync(instance.DefinitionId, cancellationToken);
        var definitionName = definition?.Name ?? "Unknown";

        return new WorkflowInstanceDto(
            instance.Id,
            instance.DefinitionId,
            definitionName,
            instance.EntityType,
            instance.EntityId,
            instance.CurrentStepName,
            instance.Status,
            instance.InitiatedBy,
            instance.CreatedAt,
            instance.CompletedAt);
    }
}

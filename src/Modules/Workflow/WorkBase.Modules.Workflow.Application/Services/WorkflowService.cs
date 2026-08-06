using WorkBase.Contracts;
using WorkBase.Modules.Workflow.Application.Contracts;

namespace WorkBase.Modules.Workflow.Application.Services;

/// <summary>
/// Cross-module facade for creating workflow instances by definition name.
/// </summary>
public sealed class WorkflowService(
    IWorkflowDefinitionRepository definitionRepository,
    IWorkflowEngine workflowEngine) : IWorkflowService
{
    public async Task<Guid?> CreateInstanceAsync(
        Guid tenantId,
        string definitionName,
        string entityType,
        Guid entityId,
        Guid initiatedByUserId,
        string? initialOutcome = null,
        CancellationToken cancellationToken = default)
    {
        var definition = await definitionRepository.GetByNameAsync(tenantId, definitionName, cancellationToken);
        if (definition is null)
            return null;

        var result = await workflowEngine.CreateInstanceAsync(
            tenantId, definition.Id, entityType, entityId, initiatedByUserId, initialOutcome, cancellationToken);

        return result.IsSuccess ? result.Value : null;
    }
}

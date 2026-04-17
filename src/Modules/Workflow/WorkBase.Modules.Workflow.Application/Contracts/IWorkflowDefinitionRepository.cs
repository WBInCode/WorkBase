using WorkBase.Modules.Workflow.Domain.Entities;

namespace WorkBase.Modules.Workflow.Application.Contracts;

public interface IWorkflowDefinitionRepository
{
    Task<WorkflowDefinition?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<WorkflowDefinition?> GetByNameAsync(Guid tenantId, string name, CancellationToken cancellationToken = default);
    Task<List<WorkflowDefinition>> GetAllAsync(Guid tenantId, CancellationToken cancellationToken = default);
    Task AddAsync(WorkflowDefinition definition, CancellationToken cancellationToken = default);
    void Update(WorkflowDefinition definition);
    void Remove(WorkflowDefinition definition);
}

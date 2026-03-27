using WorkBase.Modules.Workflow.Domain.Entities;

namespace WorkBase.Modules.Workflow.Application.Contracts;

public interface IWorkflowInstanceRepository
{
    Task<WorkflowInstance?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<WorkflowInstance?> GetByEntityAsync(Guid tenantId, string entityType, Guid entityId, CancellationToken cancellationToken = default);
    Task<List<WorkflowInstance>> GetActiveByEntityTypeAsync(Guid tenantId, string entityType, CancellationToken cancellationToken = default);
    Task AddAsync(WorkflowInstance instance, CancellationToken cancellationToken = default);
    void Update(WorkflowInstance instance);
}

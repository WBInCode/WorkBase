using WorkBase.Modules.Workflow.Domain.Entities;

namespace WorkBase.Modules.Workflow.Application.Contracts;

public interface IWorkflowActionRepository
{
    Task AddAsync(WorkflowAction action, CancellationToken cancellationToken = default);
    Task<List<WorkflowAction>> GetByStepAsync(Guid stepId, CancellationToken cancellationToken = default);
    void Update(WorkflowAction action);
}

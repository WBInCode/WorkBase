using WorkBase.Modules.Workflow.Domain.Entities;

namespace WorkBase.Modules.Workflow.Application.Contracts;

public interface IWorkflowStepRepository
{
    Task<WorkflowStep?> GetActiveStepAsync(Guid instanceId, CancellationToken cancellationToken = default);
    Task<List<WorkflowStep>> GetStepsByInstanceAsync(Guid instanceId, CancellationToken cancellationToken = default);
    Task AddAsync(WorkflowStep step, CancellationToken cancellationToken = default);
    void Update(WorkflowStep step);
}

using WorkBase.Modules.Workflow.Domain.Entities;

namespace WorkBase.Modules.Workflow.Application.Contracts;

public interface IWorkflowBranchRepository
{
    Task<WorkflowBranch?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<List<WorkflowBranch>> GetByInstanceAndGatewayAsync(Guid instanceId, string gatewayStepName, CancellationToken ct = default);
    Task<List<WorkflowBranch>> GetActiveByInstanceAsync(Guid instanceId, CancellationToken ct = default);
    Task AddAsync(WorkflowBranch branch, CancellationToken ct = default);
    void Update(WorkflowBranch branch);
}

using Microsoft.EntityFrameworkCore;
using WorkBase.Infrastructure.Persistence;
using WorkBase.Modules.Workflow.Application.Contracts;
using WorkBase.Modules.Workflow.Domain.Entities;

namespace WorkBase.Modules.Workflow.Infrastructure.Repositories;

public sealed class WorkflowBranchRepository(WorkBaseDbContext dbContext) : IWorkflowBranchRepository
{
    public async Task<WorkflowBranch?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        return await dbContext.Set<WorkflowBranch>().FindAsync([id], ct);
    }

    public async Task<List<WorkflowBranch>> GetByInstanceAndGatewayAsync(Guid instanceId, string gatewayStepName, CancellationToken ct = default)
    {
        return await dbContext.Set<WorkflowBranch>()
            .Where(b => b.InstanceId == instanceId && b.GatewayStepName == gatewayStepName)
            .ToListAsync(ct);
    }

    public async Task<List<WorkflowBranch>> GetActiveByInstanceAsync(Guid instanceId, CancellationToken ct = default)
    {
        return await dbContext.Set<WorkflowBranch>()
            .Where(b => b.InstanceId == instanceId && b.Status == "Active")
            .ToListAsync(ct);
    }

    public async Task AddAsync(WorkflowBranch branch, CancellationToken ct = default)
    {
        await dbContext.Set<WorkflowBranch>().AddAsync(branch, ct);
    }

    public void Update(WorkflowBranch branch)
    {
        dbContext.Set<WorkflowBranch>().Update(branch);
    }
}

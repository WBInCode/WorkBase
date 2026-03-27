using Microsoft.EntityFrameworkCore;
using WorkBase.Infrastructure.Persistence;
using WorkBase.Modules.Workflow.Application.Contracts;
using WorkBase.Modules.Workflow.Domain.Entities;

namespace WorkBase.Modules.Workflow.Infrastructure.Repositories;

public sealed class WorkflowStepRepository(WorkBaseDbContext dbContext) : IWorkflowStepRepository
{
    public async Task<WorkflowStep?> GetActiveStepAsync(Guid instanceId, CancellationToken cancellationToken = default)
    {
        return await dbContext.Set<WorkflowStep>()
            .Where(s => s.InstanceId == instanceId && s.Status == "Active")
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<List<WorkflowStep>> GetStepsByInstanceAsync(Guid instanceId, CancellationToken cancellationToken = default)
    {
        return await dbContext.Set<WorkflowStep>()
            .Where(s => s.InstanceId == instanceId)
            .OrderBy(s => s.EnteredAt)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(WorkflowStep step, CancellationToken cancellationToken = default)
    {
        await dbContext.Set<WorkflowStep>().AddAsync(step, cancellationToken);
    }

    public void Update(WorkflowStep step)
    {
        dbContext.Set<WorkflowStep>().Update(step);
    }
}

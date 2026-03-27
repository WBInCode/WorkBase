using Microsoft.EntityFrameworkCore;
using WorkBase.Infrastructure.Persistence;
using WorkBase.Modules.Workflow.Application.Contracts;
using WorkBase.Modules.Workflow.Domain.Entities;

namespace WorkBase.Modules.Workflow.Infrastructure.Repositories;

public sealed class WorkflowActionRepository(WorkBaseDbContext dbContext) : IWorkflowActionRepository
{
    public async Task AddAsync(WorkflowAction action, CancellationToken cancellationToken = default)
    {
        await dbContext.Set<WorkflowAction>().AddAsync(action, cancellationToken);
    }

    public async Task<List<WorkflowAction>> GetByStepAsync(Guid stepId, CancellationToken cancellationToken = default)
    {
        return await dbContext.Set<WorkflowAction>()
            .Where(a => a.StepId == stepId)
            .OrderBy(a => a.ExecutedAt)
            .ToListAsync(cancellationToken);
    }

    public void Update(WorkflowAction action)
    {
        dbContext.Set<WorkflowAction>().Update(action);
    }
}

using Microsoft.EntityFrameworkCore;
using WorkBase.Infrastructure.Persistence;
using WorkBase.Modules.Workflow.Application.Contracts;
using WorkBase.Modules.Workflow.Domain.Entities;

namespace WorkBase.Modules.Workflow.Infrastructure.Repositories;

public sealed class WorkflowInstanceRepository(WorkBaseDbContext dbContext) : IWorkflowInstanceRepository
{
    public async Task<WorkflowInstance?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await dbContext.Set<WorkflowInstance>()
            .FirstOrDefaultAsync(i => i.Id == id, cancellationToken);
    }

    public async Task<WorkflowInstance?> GetByEntityAsync(Guid tenantId, string entityType, Guid entityId, CancellationToken cancellationToken = default)
    {
        return await dbContext.Set<WorkflowInstance>()
            .Where(i => i.TenantId == tenantId && i.EntityType == entityType && i.EntityId == entityId && i.Status == "Active")
            .OrderByDescending(i => i.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<List<WorkflowInstance>> GetActiveByEntityTypeAsync(Guid tenantId, string entityType, CancellationToken cancellationToken = default)
    {
        return await dbContext.Set<WorkflowInstance>()
            .Where(i => i.TenantId == tenantId && i.EntityType == entityType && i.Status == "Active")
            .OrderByDescending(i => i.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(WorkflowInstance instance, CancellationToken cancellationToken = default)
    {
        await dbContext.Set<WorkflowInstance>().AddAsync(instance, cancellationToken);
    }

    public void Update(WorkflowInstance instance)
    {
        dbContext.Set<WorkflowInstance>().Update(instance);
    }
}

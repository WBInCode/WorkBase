using Microsoft.EntityFrameworkCore;
using WorkBase.Infrastructure.Persistence;
using WorkBase.Modules.Workflow.Application.Contracts;
using WorkBase.Modules.Workflow.Domain.Entities;

namespace WorkBase.Modules.Workflow.Infrastructure.Repositories;

public sealed class WorkflowDefinitionRepository(WorkBaseDbContext dbContext) : IWorkflowDefinitionRepository
{
    public async Task<WorkflowDefinition?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await dbContext.Set<WorkflowDefinition>()
            .FirstOrDefaultAsync(d => d.Id == id, cancellationToken);
    }

    public async Task<WorkflowDefinition?> GetByNameAsync(Guid tenantId, string name, CancellationToken cancellationToken = default)
    {
        return await dbContext.Set<WorkflowDefinition>()
            .FirstOrDefaultAsync(d => d.TenantId == tenantId && d.Name == name, cancellationToken);
    }

    public async Task<List<WorkflowDefinition>> GetAllAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        return await dbContext.Set<WorkflowDefinition>()
            .Where(d => d.TenantId == tenantId)
            .OrderBy(d => d.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(WorkflowDefinition definition, CancellationToken cancellationToken = default)
    {
        await dbContext.Set<WorkflowDefinition>().AddAsync(definition, cancellationToken);
    }

    public void Update(WorkflowDefinition definition)
    {
        dbContext.Set<WorkflowDefinition>().Update(definition);
    }

    public void Remove(WorkflowDefinition definition)
    {
        dbContext.Set<WorkflowDefinition>().Remove(definition);
    }
}

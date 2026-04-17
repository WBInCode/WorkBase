using Microsoft.EntityFrameworkCore;
using WorkBase.Infrastructure.Persistence;
using WorkBase.Modules.Tasks.Application.Contracts;
using WorkBase.Modules.Tasks.Domain.Entities;

namespace WorkBase.Modules.Tasks.Infrastructure.Repositories;

public sealed class TaskStatusTransitionRepository(WorkBaseDbContext dbContext) : ITaskStatusTransitionRepository
{
    public async Task<bool> IsTransitionAllowedAsync(Guid tenantId, Guid fromStatusId, Guid toStatusId, CancellationToken cancellationToken = default)
        => await dbContext.Set<TaskStatusTransition>()
            .AnyAsync(t => t.TenantId == tenantId
                && t.FromStatusId == fromStatusId
                && t.ToStatusId == toStatusId
                && t.IsActive, cancellationToken);

    public async Task<List<TaskStatusTransition>> GetByTenantAsync(Guid tenantId, CancellationToken cancellationToken = default)
        => await dbContext.Set<TaskStatusTransition>()
            .Where(t => t.TenantId == tenantId && t.IsActive)
            .ToListAsync(cancellationToken);
}

using Microsoft.EntityFrameworkCore;
using WorkBase.Infrastructure.Persistence;
using WorkBase.Modules.Tasks.Application.Contracts;
using WorkBase.Modules.Tasks.Domain.Entities;

namespace WorkBase.Modules.Tasks.Infrastructure.Repositories;

public sealed class TaskPriorityRepository(WorkBaseDbContext dbContext) : ITaskPriorityRepository
{
    public async Task<TaskPriority?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => await dbContext.Set<TaskPriority>().FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

    public async Task<List<TaskPriority>> GetByTenantAsync(Guid tenantId, CancellationToken cancellationToken = default)
        => await dbContext.Set<TaskPriority>()
            .Where(p => p.TenantId == tenantId && p.IsActive)
            .OrderBy(p => p.SortOrder)
            .ToListAsync(cancellationToken);
}

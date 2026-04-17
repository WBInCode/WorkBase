using Microsoft.EntityFrameworkCore;
using WorkBase.Infrastructure.Persistence;
using WorkBase.Modules.Tasks.Application.Contracts;
using WorkBase.Modules.Tasks.Domain.Entities;

namespace WorkBase.Modules.Tasks.Infrastructure.Repositories;

public sealed class TaskHistoryRepository(WorkBaseDbContext dbContext) : ITaskHistoryRepository
{
    public async Task<List<TaskHistoryEntry>> GetByTaskAsync(Guid tenantId, Guid taskId, CancellationToken cancellationToken = default)
        => await dbContext.Set<TaskHistoryEntry>()
            .Where(h => h.TenantId == tenantId && h.TaskId == taskId)
            .OrderByDescending(h => h.ChangedAt)
            .ToListAsync(cancellationToken);

    public async Task AddAsync(TaskHistoryEntry entry, CancellationToken cancellationToken = default)
        => await dbContext.Set<TaskHistoryEntry>().AddAsync(entry, cancellationToken);
}

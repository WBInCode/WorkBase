using Microsoft.EntityFrameworkCore;
using WorkBase.Infrastructure.Persistence;
using WorkBase.Modules.Tasks.Application.Contracts;
using TaskStatus = WorkBase.Modules.Tasks.Domain.Entities.TaskStatus;

namespace WorkBase.Modules.Tasks.Infrastructure.Repositories;

public sealed class TaskStatusRepository(WorkBaseDbContext dbContext) : ITaskStatusRepository
{
    public async Task<TaskStatus?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => await dbContext.Set<TaskStatus>().FirstOrDefaultAsync(s => s.Id == id, cancellationToken);

    public async Task<TaskStatus?> GetDefaultAsync(Guid tenantId, CancellationToken cancellationToken = default)
        => await dbContext.Set<TaskStatus>()
            .FirstOrDefaultAsync(s => s.TenantId == tenantId && s.IsDefault && s.IsActive, cancellationToken);

    public async Task<List<TaskStatus>> GetByTenantAsync(Guid tenantId, CancellationToken cancellationToken = default)
        => await dbContext.Set<TaskStatus>()
            .Where(s => s.TenantId == tenantId && s.IsActive)
            .OrderBy(s => s.SortOrder)
            .ToListAsync(cancellationToken);

    public async Task AddAsync(TaskStatus status, CancellationToken cancellationToken = default)
        => await dbContext.Set<TaskStatus>().AddAsync(status, cancellationToken);

    public void Remove(TaskStatus status)
        => dbContext.Set<TaskStatus>().Remove(status);
}

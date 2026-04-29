using Microsoft.EntityFrameworkCore;
using WorkBase.Infrastructure.Persistence;
using WorkBase.Modules.Tasks.Application.Contracts;
using WorkBase.Modules.Tasks.Domain.Entities;

namespace WorkBase.Modules.Tasks.Infrastructure.Repositories;

public sealed class TaskItemRepository(WorkBaseDbContext dbContext) : ITaskItemRepository
{
    public async Task<TaskItem?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => await dbContext.Set<TaskItem>().FirstOrDefaultAsync(t => t.Id == id, cancellationToken);

    public async Task<List<TaskItem>> GetByAssigneeAsync(Guid tenantId, Guid assigneeId, CancellationToken cancellationToken = default)
        => await dbContext.Set<TaskItem>()
            .Where(t => t.TenantId == tenantId
                && (t.AssigneeId == assigneeId
                    || t.AdditionalAssignees.Any(a => a.EmployeeId == assigneeId)))
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync(cancellationToken);

    public async Task<List<TaskItem>> GetByTenantAsync(Guid tenantId, CancellationToken cancellationToken = default)
        => await dbContext.Set<TaskItem>()
            .Where(t => t.TenantId == tenantId)
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync(cancellationToken);

    public async Task AddAsync(TaskItem task, CancellationToken cancellationToken = default)
        => await dbContext.Set<TaskItem>().AddAsync(task, cancellationToken);

    public void Update(TaskItem task)
        => dbContext.Set<TaskItem>().Update(task);

    public void Remove(TaskItem task)
        => dbContext.Set<TaskItem>().Remove(task);
}

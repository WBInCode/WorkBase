using Microsoft.EntityFrameworkCore;
using WorkBase.Infrastructure.Persistence;
using WorkBase.Modules.Tasks.Application.Contracts;
using WorkBase.Modules.Tasks.Domain.Entities;

namespace WorkBase.Modules.Tasks.Infrastructure.Repositories;

public sealed class TaskCommentRepository(WorkBaseDbContext dbContext) : ITaskCommentRepository
{
    public async Task<TaskComment?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => await dbContext.Set<TaskComment>().FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

    public async Task<List<TaskComment>> GetByTaskAsync(Guid tenantId, Guid taskId, CancellationToken cancellationToken = default)
        => await dbContext.Set<TaskComment>()
            .Where(c => c.TenantId == tenantId && c.TaskId == taskId)
            .OrderBy(c => c.CreatedAt)
            .ToListAsync(cancellationToken);

    public async Task AddAsync(TaskComment comment, CancellationToken cancellationToken = default)
        => await dbContext.Set<TaskComment>().AddAsync(comment, cancellationToken);

    public void Remove(TaskComment comment)
        => dbContext.Set<TaskComment>().Remove(comment);
}

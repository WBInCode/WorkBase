using Microsoft.EntityFrameworkCore;
using WorkBase.Infrastructure.Persistence;
using WorkBase.Modules.Tasks.Application.Contracts;
using WorkBase.Modules.Tasks.Domain.Entities;

namespace WorkBase.Modules.Tasks.Infrastructure.Repositories;

public sealed class TaskAttachmentRepository(WorkBaseDbContext dbContext) : ITaskAttachmentRepository
{
    public async Task<TaskAttachment?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => await dbContext.Set<TaskAttachment>().FirstOrDefaultAsync(a => a.Id == id, cancellationToken);

    public async Task<List<TaskAttachment>> GetByTaskAsync(Guid tenantId, Guid taskId, CancellationToken cancellationToken = default)
        => await dbContext.Set<TaskAttachment>()
            .Where(a => a.TenantId == tenantId && a.TaskId == taskId)
            .OrderByDescending(a => a.UploadedAt)
            .ToListAsync(cancellationToken);

    public async Task AddAsync(TaskAttachment attachment, CancellationToken cancellationToken = default)
        => await dbContext.Set<TaskAttachment>().AddAsync(attachment, cancellationToken);
}

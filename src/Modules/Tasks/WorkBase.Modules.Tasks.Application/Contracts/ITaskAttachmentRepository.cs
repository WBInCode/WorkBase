using WorkBase.Modules.Tasks.Domain.Entities;

namespace WorkBase.Modules.Tasks.Application.Contracts;

public interface ITaskAttachmentRepository
{
    Task<TaskAttachment?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<List<TaskAttachment>> GetByTaskAsync(Guid tenantId, Guid taskId, CancellationToken cancellationToken = default);
    Task AddAsync(TaskAttachment attachment, CancellationToken cancellationToken = default);
}

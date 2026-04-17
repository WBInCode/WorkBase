using WorkBase.Modules.Tasks.Domain.Entities;

namespace WorkBase.Modules.Tasks.Application.Contracts;

public interface ITaskCommentRepository
{
    Task<TaskComment?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<List<TaskComment>> GetByTaskAsync(Guid tenantId, Guid taskId, CancellationToken cancellationToken = default);
    Task AddAsync(TaskComment comment, CancellationToken cancellationToken = default);
    void Remove(TaskComment comment);
}

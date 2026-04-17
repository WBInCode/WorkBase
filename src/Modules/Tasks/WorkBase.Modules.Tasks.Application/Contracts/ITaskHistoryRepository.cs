using WorkBase.Modules.Tasks.Domain.Entities;

namespace WorkBase.Modules.Tasks.Application.Contracts;

public interface ITaskHistoryRepository
{
    Task<List<TaskHistoryEntry>> GetByTaskAsync(Guid tenantId, Guid taskId, CancellationToken cancellationToken = default);
    Task AddAsync(TaskHistoryEntry entry, CancellationToken cancellationToken = default);
}

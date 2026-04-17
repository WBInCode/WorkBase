using WorkBase.Modules.Tasks.Domain.Entities;

namespace WorkBase.Modules.Tasks.Application.Contracts;

public interface ITaskPriorityRepository
{
    Task<TaskPriority?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<List<TaskPriority>> GetByTenantAsync(Guid tenantId, CancellationToken cancellationToken = default);
    Task AddAsync(TaskPriority priority, CancellationToken cancellationToken = default);
    void Remove(TaskPriority priority);
}

using WorkBase.Modules.Tasks.Domain.Entities;
using TaskStatus = WorkBase.Modules.Tasks.Domain.Entities.TaskStatus;

namespace WorkBase.Modules.Tasks.Application.Contracts;

public interface ITaskStatusRepository
{
    Task<TaskStatus?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<TaskStatus?> GetDefaultAsync(Guid tenantId, CancellationToken cancellationToken = default);
    Task<List<TaskStatus>> GetByTenantAsync(Guid tenantId, CancellationToken cancellationToken = default);
}

using WorkBase.Modules.Tasks.Domain.Entities;

namespace WorkBase.Modules.Tasks.Application.Contracts;

public interface ITaskStatusTransitionRepository
{
    Task<bool> IsTransitionAllowedAsync(Guid tenantId, Guid fromStatusId, Guid toStatusId, CancellationToken cancellationToken = default);
    Task<List<TaskStatusTransition>> GetByTenantAsync(Guid tenantId, CancellationToken cancellationToken = default);

    /// <summary>Czy najemca w ogóle zdefiniował dozwolone przejścia.</summary>
    Task<bool> HasAnyAsync(Guid tenantId, CancellationToken cancellationToken = default);
}

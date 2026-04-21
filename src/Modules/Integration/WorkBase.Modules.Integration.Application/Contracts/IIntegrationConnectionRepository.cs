using WorkBase.Modules.Integration.Domain.Entities;
using WorkBase.Modules.Integration.Domain.Enums;

namespace WorkBase.Modules.Integration.Application.Contracts;

public interface IIntegrationConnectionRepository
{
    Task<IntegrationConnection?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IntegrationConnection?> GetByUserAndProviderAsync(Guid tenantId, Guid userId, IntegrationProvider provider, CancellationToken ct = default);
    Task<IReadOnlyList<IntegrationConnection>> GetByUserAsync(Guid tenantId, Guid userId, CancellationToken ct = default);
    Task AddAsync(IntegrationConnection connection, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}

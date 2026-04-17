using WorkBase.Modules.Identity.Domain.Entities;

namespace WorkBase.Modules.Identity.Application.Contracts;

public interface IFeatureFlagService
{
    Task<List<FeatureFlag>> GetByTenantAsync(Guid tenantId, CancellationToken ct = default);
    Task ToggleAsync(Guid tenantId, string module, string? userId, CancellationToken ct = default);
}

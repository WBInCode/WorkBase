using WorkBase.Shared.Domain;

namespace WorkBase.Shared.Domain;

/// <summary>
/// Service for reading/writing per-tenant configuration values.
/// </summary>
public interface ITenantConfigService
{
    Task<string?> GetAsync(Guid tenantId, string key, CancellationToken cancellationToken = default);
    Task<T?> GetAsync<T>(Guid tenantId, string key, CancellationToken cancellationToken = default) where T : class;

    /// <summary>
    /// Returns all config rows for the tenant whose key starts with <paramref name="keyPrefix"/>,
    /// keyed by the full (still-prefixed) key. Used for bulk lookups like terminology overrides.
    /// </summary>
    Task<Dictionary<string, string>> GetAllAsync(Guid tenantId, string keyPrefix, CancellationToken cancellationToken = default);
    Task SetAsync(Guid tenantId, string key, string value, CancellationToken cancellationToken = default);
    Task SetAsync<T>(Guid tenantId, string key, T value, CancellationToken cancellationToken = default) where T : class;
    Task DeleteAsync(Guid tenantId, string key, CancellationToken cancellationToken = default);
}

using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using WorkBase.Infrastructure.Persistence;
using WorkBase.Infrastructure.Persistence.Entities;
using WorkBase.Shared.Domain;

namespace WorkBase.Infrastructure.Services;

public sealed class TenantConfigService(WorkBaseDbContext db) : ITenantConfigService
{
    public async Task<string?> GetAsync(Guid tenantId, string key, CancellationToken cancellationToken = default)
    {
        var config = await db.Set<TenantConfig>()
            .FirstOrDefaultAsync(c => c.TenantId == tenantId && c.Key == key, cancellationToken);
        return config?.Value;
    }

    public async Task<T?> GetAsync<T>(Guid tenantId, string key, CancellationToken cancellationToken = default) where T : class
    {
        var value = await GetAsync(tenantId, key, cancellationToken);
        return value is not null ? JsonSerializer.Deserialize<T>(value) : null;
    }

    public async Task<Dictionary<string, string>> GetAllAsync(Guid tenantId, string keyPrefix, CancellationToken cancellationToken = default)
    {
        return await db.Set<TenantConfig>()
            .Where(c => c.TenantId == tenantId && c.Key.StartsWith(keyPrefix))
            .ToDictionaryAsync(c => c.Key, c => c.Value, cancellationToken);
    }

    public async Task SetAsync(Guid tenantId, string key, string value, CancellationToken cancellationToken = default)
    {
        var config = await db.Set<TenantConfig>()
            .FirstOrDefaultAsync(c => c.TenantId == tenantId && c.Key == key, cancellationToken);

        if (config is null)
        {
            config = new TenantConfig
            {
                Id = Guid.CreateVersion7(),
                TenantId = tenantId,
                Key = key,
                Value = value,
                UpdatedAt = DateTime.UtcNow
            };
            db.Set<TenantConfig>().Add(config);
        }
        else
        {
            config.Value = value;
            config.UpdatedAt = DateTime.UtcNow;
        }

        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task SetAsync<T>(Guid tenantId, string key, T value, CancellationToken cancellationToken = default) where T : class
    {
        await SetAsync(tenantId, key, JsonSerializer.Serialize(value), cancellationToken);
    }

    public async Task DeleteAsync(Guid tenantId, string key, CancellationToken cancellationToken = default)
    {
        var config = await db.Set<TenantConfig>()
            .FirstOrDefaultAsync(c => c.TenantId == tenantId && c.Key == key, cancellationToken);

        if (config is not null)
        {
            db.Set<TenantConfig>().Remove(config);
            await db.SaveChangesAsync(cancellationToken);
        }
    }
}

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using WorkBase.Infrastructure.Persistence;
using WorkBase.Modules.Identity.Domain.Entities;
using WorkBase.Shared.Auth;

namespace WorkBase.Infrastructure.Auth;

public sealed class DataScopeService(WorkBaseDbContext dbContext, IMemoryCache cache) : IDataScopeService
{
    private static string CacheKey(Guid userId, Guid tenantId, string module)
        => $"datascope:{tenantId}:{userId}:{module}";

    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(5);

    public async Task<DataScopeResult> GetEffectiveScopeAsync(
        Guid userId, Guid tenantId, string module, CancellationToken ct = default)
    {
        var key = CacheKey(userId, tenantId, module);
        if (cache.TryGetValue<DataScopeResult>(key, out var cached) && cached is not null)
            return cached;

        var internalUserId = await dbContext.Set<User>()
            .Where(u => u.Id == userId || u.KeycloakId == userId.ToString())
            .Select(u => u.Id)
            .FirstOrDefaultAsync(ct);

        if (internalUserId == Guid.Empty)
        {
            var fallback = new DataScopeResult(DataScopeLevelValue.Own);
            cache.Set(key, fallback, CacheDuration);
            return fallback;
        }

        var userRoleIds = await dbContext.Set<UserRole>()
            .Where(ur => ur.UserId == internalUserId && ur.TenantId == tenantId)
            .Select(ur => ur.RoleId)
            .ToListAsync(ct);

        if (userRoleIds.Count == 0)
        {
            var fallback = new DataScopeResult(DataScopeLevelValue.Own);
            cache.Set(key, fallback, CacheDuration);
            return fallback;
        }

        var scopeLevel = await dbContext.Set<DataScope>()
            .Where(ds => userRoleIds.Contains(ds.RoleId) && ds.Module == module && ds.TenantId == tenantId)
            .Select(ds => (int)ds.ScopeLevel)
            .DefaultIfEmpty(0)
            .MaxAsync(ct);

        var result = new DataScopeResult((DataScopeLevelValue)scopeLevel);
        cache.Set(key, result, CacheDuration);
        return result;
    }
}

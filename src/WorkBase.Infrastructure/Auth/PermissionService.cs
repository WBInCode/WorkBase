using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using WorkBase.Infrastructure.Persistence;
using WorkBase.Modules.Identity.Domain.Entities;
using WorkBase.Shared.Auth;

namespace WorkBase.Infrastructure.Auth;

public sealed class PermissionService(
    WorkBaseDbContext dbContext,
    IMemoryCache cache) : IPermissionService
{
    private static string CacheKey(Guid userId, Guid tenantId) => $"permissions:{tenantId}:{userId}";
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(5);

    public async Task<IReadOnlySet<string>> GetUserPermissionsAsync(
        Guid userId,
        Guid tenantId,
        CancellationToken cancellationToken = default)
    {
        var key = CacheKey(userId, tenantId);

        if (cache.TryGetValue<IReadOnlySet<string>>(key, out var cached) && cached is not null)
            return cached;

        var permissions = await dbContext.Set<UserRole>()
            .Where(ur => ur.UserId == userId && ur.TenantId == tenantId)
            .Join(
                dbContext.Set<Role>().Where(r => r.IsActive),
                ur => ur.RoleId,
                r => r.Id,
                (ur, r) => r.Id)
            .Join(
                dbContext.Set<RolePermission>(),
                roleId => roleId,
                rp => rp.RoleId,
                (_, rp) => rp.PermissionId)
            .Join(
                dbContext.Set<Permission>(),
                permId => permId,
                p => p.Id,
                (_, p) => p.Scope != null
                    ? p.Module + "." + p.Action + "." + p.Scope
                    : p.Module + "." + p.Action)
            .Distinct()
            .ToListAsync(cancellationToken);

        var result = (IReadOnlySet<string>)permissions.ToHashSet();

        cache.Set(key, result, CacheDuration);

        return result;
    }

    public async Task<bool> HasPermissionAsync(
        Guid userId,
        Guid tenantId,
        string permission,
        CancellationToken cancellationToken = default)
    {
        var permissions = await GetUserPermissionsAsync(userId, tenantId, cancellationToken);
        return permissions.Contains(permission);
    }

    public static void InvalidateCache(IMemoryCache cache, Guid userId, Guid tenantId)
    {
        cache.Remove(CacheKey(userId, tenantId));
    }
}

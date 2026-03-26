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

        // userId may be either the internal User.Id or the Keycloak sub (parsed as Guid).
        // Resolve to internal User.Id via the users table so UserRole join works.
        var internalUserId = await dbContext.Set<User>()
            .Where(u => u.Id == userId || u.KeycloakId == userId.ToString())
            .Select(u => u.Id)
            .FirstOrDefaultAsync(cancellationToken);

        if (internalUserId == Guid.Empty)
        {
            var empty = (IReadOnlySet<string>)new HashSet<string>();
            cache.Set(key, empty, CacheDuration);
            return empty;
        }

        var permissions = await dbContext.Set<UserRole>()
            .Where(ur => ur.UserId == internalUserId && ur.TenantId == tenantId)
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

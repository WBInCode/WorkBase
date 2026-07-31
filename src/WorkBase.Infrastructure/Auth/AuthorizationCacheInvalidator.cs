using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using WorkBase.Infrastructure.Persistence;
using WorkBase.Modules.Identity.Domain.Entities;
using WorkBase.Shared.Auth;
using WorkBase.Shared.Modules;

namespace WorkBase.Infrastructure.Auth;

/// <inheritdoc cref="IAuthorizationCacheInvalidator" />
public sealed class AuthorizationCacheInvalidator(
    WorkBaseDbContext dbContext,
    IMemoryCache cache) : IAuthorizationCacheInvalidator
{
    public Task InvalidateUserAsync(Guid userId, Guid tenantId, CancellationToken ct = default)
        => InvalidateUsersAsync([userId], tenantId, ct);

    public async Task InvalidateRoleAsync(Guid roleId, Guid tenantId, CancellationToken ct = default)
    {
        var userIds = await dbContext.Set<UserRole>()
            .Where(userRole => userRole.RoleId == roleId && userRole.TenantId == tenantId)
            .Select(userRole => userRole.UserId)
            .Distinct()
            .ToListAsync(ct);

        await InvalidateUsersAsync(userIds, tenantId, ct);
    }

    private async Task InvalidateUsersAsync(IReadOnlyCollection<Guid> userIds, Guid tenantId, CancellationToken ct)
    {
        if (userIds.Count == 0)
        {
            return;
        }

        // Klucze buduje sie z identyfikatora, ktory przyszedl w zadaniu, a ten bywa albo
        // wewnetrznym User.Id, albo `sub` z Keycloaka (zaleznie od sciezki wywolania).
        // Ta sama osoba ma wiec do dwoch wpisow i skasowanie tylko jednego zostawiloby
        // drugi — czyli dokladnie ten, ktorego uzywaja zwykle zadania z tokenem.
        var identities = await dbContext.Set<User>()
            .Where(user => user.TenantId == tenantId && userIds.Contains(user.Id))
            .Select(user => new { user.Id, user.KeycloakId })
            .ToListAsync(ct);

        var keyIds = new HashSet<Guid>(userIds);
        foreach (var identity in identities)
        {
            keyIds.Add(identity.Id);
            if (Guid.TryParse(identity.KeycloakId, out var keycloakId))
            {
                keyIds.Add(keycloakId);
            }
        }

        foreach (var id in keyIds)
        {
            cache.Remove(PermissionService.CacheKey(id, tenantId));

            // Zakresy danych sa zapamietane osobno dla kazdego modulu, wiec nie ma
            // jednego klucza do usuniecia — trzeba przejsc po katalogu modulow.
            foreach (var module in ModuleCatalog.All)
            {
                cache.Remove(EmployeeScopeResolver.CacheKey(id, tenantId, module.Key));
                cache.Remove(DataScopeService.CacheKey(id, tenantId, module.Key));
            }
        }
    }
}

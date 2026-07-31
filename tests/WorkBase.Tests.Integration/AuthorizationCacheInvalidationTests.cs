using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using WorkBase.Infrastructure.Auth;
using WorkBase.Infrastructure.Persistence;
using WorkBase.Modules.Identity.Domain.Entities;
using Xunit;

namespace WorkBase.Tests.Integration;

/// <summary>
/// Sprawdza, ze zmiana rol natychmiast przestaje byc zaslaniana przez pamiec podreczna.
/// </summary>
/// <remarks>
/// Uprawnienia byly zapamietywane na 5 minut, a nic ich nie czyscilo — metoda
/// InvalidateCache istniala, ale nie byla wolana z zadnego miejsca w kodzie.
/// Administrator nadawal role i przez kilka minut widzial, ze "nic sie nie stalo".
/// </remarks>
public sealed class AuthorizationCacheInvalidationTests
{
    private static readonly Guid TenantId = Guid.Parse("10000000-0000-0000-0000-000000000001");

    [Fact]
    public async Task Nadanie_roli_od_razu_odslania_nowe_uprawnienia()
    {
        await using var db = CreateDbContext();
        var cache = new MemoryCache(new MemoryCacheOptions());

        var role = Role.Create(TenantId, "Kierownik", RoleType.Organizational, level: 10);
        var permission = Permission.Create("time", "approve", null, "Akceptacja kart czasu pracy");
        var user = User.Create("kierownik", "kierownik@example.com", "Test", "Kierownik", TenantId);
        db.AddRange(role, permission, user);
        await db.SaveChangesAsync();
        db.Add(RolePermission.Create(role.Id, permission.Id));
        await db.SaveChangesAsync();

        var permissions = new PermissionService(db, cache);
        var przedNadaniem = await permissions.GetUserPermissionsAsync(user.Id, TenantId);
        Assert.Empty(przedNadaniem);

        var roleService = new RoleManagementService(db, new AuthorizationCacheInvalidator(db, cache));
        await roleService.AssignUserRoleAsync(user.Id, role.Id, TenantId, "test");

        var poNadaniu = await permissions.GetUserPermissionsAsync(user.Id, TenantId);
        Assert.Contains("time.approve", poNadaniu);
    }

    [Fact]
    public async Task Czysci_wpis_zapisany_pod_identyfikatorem_z_Keycloaka()
    {
        // Klucz cache powstaje z identyfikatora podanego w zadaniu, a zwykle zadanie
        // przynosi `sub` z tokenu, nie wewnetrzne User.Id. Gdyby czyszczenie obejmowalo
        // tylko jedna postac, realny ruch dalej dostawalby stara odpowiedz.
        await using var db = CreateDbContext();
        var cache = new MemoryCache(new MemoryCacheOptions());

        var keycloakId = Guid.NewGuid();
        var role = Role.Create(TenantId, "Kierownik", RoleType.Organizational, level: 10);
        var permission = Permission.Create("time", "approve", null, "Akceptacja kart czasu pracy");
        var user = User.Create(keycloakId.ToString(), "kierownik2@example.com", "Test", "Kierownik", TenantId);
        db.AddRange(role, permission, user);
        await db.SaveChangesAsync();
        db.Add(RolePermission.Create(role.Id, permission.Id));
        await db.SaveChangesAsync();

        var permissions = new PermissionService(db, cache);
        Assert.Empty(await permissions.GetUserPermissionsAsync(keycloakId, TenantId));

        var roleService = new RoleManagementService(db, new AuthorizationCacheInvalidator(db, cache));
        await roleService.AssignUserRoleAsync(user.Id, role.Id, TenantId, "test");

        var poNadaniu = await permissions.GetUserPermissionsAsync(keycloakId, TenantId);
        Assert.Contains("time.approve", poNadaniu);
    }

    [Fact]
    public async Task Odebranie_roli_od_razu_zabiera_uprawnienia()
    {
        await using var db = CreateDbContext();
        var cache = new MemoryCache(new MemoryCacheOptions());

        var role = Role.Create(TenantId, "Kierownik", RoleType.Organizational, level: 10);
        var permission = Permission.Create("time", "approve", null, "Akceptacja kart czasu pracy");
        var user = User.Create("kierownik3", "kierownik3@example.com", "Test", "Kierownik", TenantId);
        db.AddRange(role, permission, user);
        await db.SaveChangesAsync();
        db.AddRange(
            RolePermission.Create(role.Id, permission.Id),
            UserRole.Create(user.Id, role.Id, TenantId, "test"));
        await db.SaveChangesAsync();

        var permissions = new PermissionService(db, cache);
        Assert.Contains("time.approve", await permissions.GetUserPermissionsAsync(user.Id, TenantId));

        var roleService = new RoleManagementService(db, new AuthorizationCacheInvalidator(db, cache));
        await roleService.UnassignUserRoleAsync(user.Id, role.Id, TenantId);

        Assert.Empty(await permissions.GetUserPermissionsAsync(user.Id, TenantId));
    }

    private static WorkBaseDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<WorkBaseDbContext>()
            .UseInMemoryDatabase($"cache-invalidation-tests-{Guid.NewGuid():N}")
            .Options;
        return new WorkBaseDbContext(options);
    }
}

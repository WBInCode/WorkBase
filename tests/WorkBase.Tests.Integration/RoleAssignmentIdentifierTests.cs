using Microsoft.EntityFrameworkCore;
using NSubstitute;
using WorkBase.Infrastructure.Auth;
using WorkBase.Infrastructure.Persistence;
using WorkBase.Modules.Identity.Domain.Entities;
using WorkBase.Shared.Auth;
using Xunit;

namespace WorkBase.Tests.Integration;

/// <summary>
/// Panel „Użytkownicy roli” podaje identyfikator wzięty z org_employees.user_id, a tam siedzi
/// `sub` z Keycloak, nie klucz z iam_users. Bez rozwiązania tego identyfikatora przypisanie roli
/// kończyło się na produkcji komunikatem „Użytkownik nie istnieje w bieżącej organizacji”.
/// </summary>
public sealed class RoleAssignmentIdentifierTests
{
    private static readonly Guid TenantId = Guid.Parse("10000000-0000-0000-0000-000000000001");

    [Fact]
    public async Task Assigning_by_Keycloak_id_stores_the_internal_user_id()
    {
        await using var db = CreateDbContext();
        var keycloakId = Guid.NewGuid();
        var role = Role.Create(TenantId, "Kierownik", RoleType.Organizational, level: 50);
        var user = User.Create(keycloakId.ToString(), "kierownik@example.com", "Kamil", "Kida", TenantId);
        db.AddRange(role, user);
        await db.SaveChangesAsync();
        var service = new RoleManagementService(db, Substitute.For<IAuthorizationCacheInvalidator>());

        await service.AssignUserRoleAsync(keycloakId, role.Id, TenantId, "manual");

        var assignment = Assert.Single(await db.Set<UserRole>().ToListAsync());
        Assert.Equal(user.Id, assignment.UserId);
        Assert.NotEqual(keycloakId, assignment.UserId);
    }

    [Fact]
    public async Task Unassigning_by_Keycloak_id_removes_the_assignment()
    {
        await using var db = CreateDbContext();
        var keycloakId = Guid.NewGuid();
        var role = Role.Create(TenantId, "Kierownik", RoleType.Organizational, level: 50);
        var user = User.Create(keycloakId.ToString(), "kierownik@example.com", "Kamil", "Kida", TenantId);
        db.AddRange(role, user);
        await db.SaveChangesAsync();
        db.Add(UserRole.Create(user.Id, role.Id, TenantId, "manual"));
        await db.SaveChangesAsync();
        var service = new RoleManagementService(db, Substitute.For<IAuthorizationCacheInvalidator>());

        await service.UnassignUserRoleAsync(keycloakId, role.Id, TenantId);

        Assert.Empty(await db.Set<UserRole>().ToListAsync());
    }

    [Fact]
    public async Task Unknown_user_is_still_rejected()
    {
        await using var db = CreateDbContext();
        var role = Role.Create(TenantId, "Kierownik", RoleType.Organizational, level: 50);
        db.Add(role);
        await db.SaveChangesAsync();
        var service = new RoleManagementService(db, Substitute.For<IAuthorizationCacheInvalidator>());

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.AssignUserRoleAsync(Guid.NewGuid(), role.Id, TenantId, "manual"));

        Assert.Contains("nie istnieje w bieżącej organizacji", exception.Message);
    }

    private static WorkBaseDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<WorkBaseDbContext>()
            .UseInMemoryDatabase($"role-assignment-tests-{Guid.NewGuid():N}")
            .Options;
        return new WorkBaseDbContext(options);
    }
}

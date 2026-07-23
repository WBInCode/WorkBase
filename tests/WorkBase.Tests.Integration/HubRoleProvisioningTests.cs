using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using WorkBase.Infrastructure.Auth;
using WorkBase.Infrastructure.Persistence;
using WorkBase.Modules.Identity.Domain.Entities;
using Xunit;

namespace WorkBase.Tests.Integration;

public sealed class HubRoleProvisioningTests
{
    private static readonly Guid CustomerTenantId =
        Guid.Parse("10000000-0000-0000-0000-000000000001");

    [Fact]
    public async Task Customer_owner_login_transfers_the_only_Admin_role()
    {
        await using var db = CreateDbContext();
        var adminRole = Role.Create(CustomerTenantId, "Admin", RoleType.System, level: 1);
        var employeeRole = Role.Create(CustomerTenantId, "Pracownik", RoleType.Organizational, level: 100);
        var superAdminRole = Role.Create(CustomerTenantId, "Super Admin", RoleType.System, level: 0);
        var previousOwner = User.Create("previous-owner", "previous@example.com", "Previous", "Owner", CustomerTenantId);
        var currentOwner = User.Create("current-owner", "current@example.com", "Current", "Owner", CustomerTenantId);

        db.AddRange(adminRole, employeeRole, superAdminRole, previousOwner, currentOwner);
        await db.SaveChangesAsync();
        db.AddRange(
            UserRole.Create(previousOwner.Id, adminRole.Id, CustomerTenantId, "system"),
            UserRole.Create(currentOwner.Id, employeeRole.Id, CustomerTenantId, "system"));
        await db.SaveChangesAsync();

        var principal = new ClaimsPrincipal(new ClaimsIdentity(
        [
            new Claim("sub", currentOwner.KeycloakId),
            new Claim("tenant_id", CustomerTenantId.ToString()),
            new Claim("hub_role", "owner"),
        ], "test"));
        var service = new UserProvisioningService(
            db, NullLogger<UserProvisioningService>.Instance);

        await service.EnsureUserProvisionedAsync(principal);

        var assignments = await db.Set<UserRole>().ToListAsync();
        Assert.DoesNotContain(assignments,
            assignment => assignment.UserId == previousOwner.Id && assignment.RoleId == adminRole.Id);
        Assert.Contains(assignments,
            assignment => assignment.UserId == currentOwner.Id && assignment.RoleId == adminRole.Id);
        Assert.DoesNotContain(assignments,
            assignment => assignment.UserId == currentOwner.Id && assignment.RoleId == employeeRole.Id);
        Assert.DoesNotContain(assignments,
            assignment => assignment.TenantId == CustomerTenantId && assignment.RoleId == superAdminRole.Id);
    }

    [Fact]
    public async Task Customer_Admin_role_cannot_be_assigned_manually()
    {
        await using var db = CreateDbContext();
        var adminRole = Role.Create(CustomerTenantId, "Admin", RoleType.System, level: 1);
        var user = User.Create("employee", "employee@example.com", "Test", "User", CustomerTenantId);
        db.AddRange(adminRole, user);
        await db.SaveChangesAsync();
        var service = new RoleManagementService(db);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.AssignUserRoleAsync(user.Id, adminRole.Id, CustomerTenantId, "manual"));

        Assert.Contains("WB Platform", exception.Message);
    }

    private static WorkBaseDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<WorkBaseDbContext>()
            .UseInMemoryDatabase($"hub-role-tests-{Guid.NewGuid():N}")
            .Options;
        return new WorkBaseDbContext(options);
    }
}
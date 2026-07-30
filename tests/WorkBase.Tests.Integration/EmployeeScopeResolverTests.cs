using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using WorkBase.Infrastructure.Auth;
using WorkBase.Infrastructure.Persistence;
using WorkBase.Modules.Identity.Domain.Entities;
using WorkBase.Modules.Organization.Domain.Entities;
using Xunit;

namespace WorkBase.Tests.Integration;

public sealed class EmployeeScopeResolverTests
{
    private static readonly Guid TenantId = Guid.Parse("10000000-0000-0000-0000-000000000001");
    private static readonly Guid SupervisorEmployeeId = Guid.Parse("20000000-0000-0000-0000-000000000001");
    private static readonly Guid SubordinateEmployeeId = Guid.Parse("20000000-0000-0000-0000-000000000002");
    private static readonly Guid StrangerEmployeeId = Guid.Parse("20000000-0000-0000-0000-000000000003");

    [Fact]
    public async Task Department_scope_reaches_only_own_subordinates()
    {
        await using var db = CreateDbContext();
        var user = await ArrangeUserWithScope(db, DataScopeLevel.Department);
        var resolver = CreateResolver(db);

        db.Add(SupervisorRelation.Create(TenantId, SupervisorEmployeeId, SubordinateEmployeeId, DateTime.UtcNow.AddDays(-30)));
        await db.SaveChangesAsync();

        Assert.True(await resolver.CanAccessEmployeeAsync(
            user.Id, TenantId, SupervisorEmployeeId, SubordinateEmployeeId, "leave"));
        Assert.False(await resolver.CanAccessEmployeeAsync(
            user.Id, TenantId, SupervisorEmployeeId, StrangerEmployeeId, "leave"));
    }

    [Fact]
    public async Task Ended_supervision_no_longer_grants_access()
    {
        await using var db = CreateDbContext();
        var user = await ArrangeUserWithScope(db, DataScopeLevel.Department);
        var resolver = CreateResolver(db);

        var relation = SupervisorRelation.Create(TenantId, SupervisorEmployeeId, SubordinateEmployeeId, DateTime.UtcNow.AddDays(-30));
        relation.End(DateTime.UtcNow.AddDays(-1));
        db.Add(relation);
        await db.SaveChangesAsync();

        Assert.False(await resolver.CanAccessEmployeeAsync(
            user.Id, TenantId, SupervisorEmployeeId, SubordinateEmployeeId, "leave"));
    }

    [Fact]
    public async Task Own_scope_reaches_nobody_else()
    {
        await using var db = CreateDbContext();
        var user = await ArrangeUserWithScope(db, DataScopeLevel.Own);
        var resolver = CreateResolver(db);

        db.Add(SupervisorRelation.Create(TenantId, SupervisorEmployeeId, SubordinateEmployeeId, DateTime.UtcNow.AddDays(-30)));
        await db.SaveChangesAsync();

        Assert.False(await resolver.CanAccessEmployeeAsync(
            user.Id, TenantId, SupervisorEmployeeId, SubordinateEmployeeId, "leave"));
    }

    [Fact]
    public async Task Organization_scope_reaches_everyone()
    {
        await using var db = CreateDbContext();
        var user = await ArrangeUserWithScope(db, DataScopeLevel.Organization);
        var resolver = CreateResolver(db);

        Assert.True(await resolver.CanAccessEmployeeAsync(
            user.Id, TenantId, SupervisorEmployeeId, StrangerEmployeeId, "leave"));
    }

    [Fact]
    public async Task Missing_scope_rows_fall_back_to_subordinates()
    {
        await using var db = CreateDbContext();
        var user = User.Create("no-scope", "no-scope@example.com", "Bez", "Zakresu", TenantId);
        var role = Role.Create(TenantId, "Kierownik", RoleType.Organizational, level: 10);
        db.AddRange(user, role);
        await db.SaveChangesAsync();
        db.Add(UserRole.Create(user.Id, role.Id, TenantId, "system"));
        db.Add(SupervisorRelation.Create(TenantId, SupervisorEmployeeId, SubordinateEmployeeId, DateTime.UtcNow.AddDays(-30)));
        await db.SaveChangesAsync();
        var resolver = CreateResolver(db);

        Assert.True(await resolver.CanAccessEmployeeAsync(
            user.Id, TenantId, SupervisorEmployeeId, SubordinateEmployeeId, "leave"));
        Assert.False(await resolver.CanAccessEmployeeAsync(
            user.Id, TenantId, SupervisorEmployeeId, StrangerEmployeeId, "leave"));
    }

    private static async Task<User> ArrangeUserWithScope(WorkBaseDbContext db, DataScopeLevel level)
    {
        var user = User.Create($"user-{level}", $"{level}@example.com", "Test", "User", TenantId);
        var role = Role.Create(TenantId, $"Rola {level}", RoleType.Organizational, level: 10);
        db.AddRange(user, role);
        await db.SaveChangesAsync();
        db.Add(UserRole.Create(user.Id, role.Id, TenantId, "system"));
        db.Add(DataScope.Create(TenantId, role.Id, "leave", level));
        await db.SaveChangesAsync();
        return user;
    }

    private static EmployeeScopeResolver CreateResolver(WorkBaseDbContext db) =>
        new(db, new MemoryCache(new MemoryCacheOptions()));

    private static WorkBaseDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<WorkBaseDbContext>()
            .UseInMemoryDatabase($"employee-scope-{Guid.NewGuid():N}")
            .Options;
        return new WorkBaseDbContext(options);
    }
}

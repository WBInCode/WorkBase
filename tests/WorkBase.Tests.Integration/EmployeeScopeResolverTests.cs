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

    // --- GetVisibleEmployeeIdsAsync: wyliczanie zakresu bez listy kandydatow ---
    //
    // Uzywa tego pulpit. Kazde z ponizszych zachowan bylo wczesniej zlamane: osiem endpointow
    // wolalo zapytanie bez zakresu, wiec kazdy pracownik widzial liczby calej firmy.

    [Fact]
    public async Task Zakres_wlasny_wypisuje_wylacznie_siebie()
    {
        await using var db = CreateDbContext();
        var user = await ArrangeUserWithScope(db, DataScopeLevel.Own, "dashboard");
        db.Add(SupervisorRelation.Create(TenantId, SupervisorEmployeeId, SubordinateEmployeeId, DateTime.UtcNow.AddDays(-30)));
        await db.SaveChangesAsync();
        var resolver = CreateResolver(db);

        var widoczni = await resolver.GetVisibleEmployeeIdsAsync(
            user.Id, TenantId, SupervisorEmployeeId, "dashboard");

        Assert.NotNull(widoczni);
        Assert.Equal([SupervisorEmployeeId], widoczni!);
    }

    [Fact]
    public async Task Zakres_dzialu_wypisuje_siebie_i_podwladnych_ale_nie_obcych()
    {
        await using var db = CreateDbContext();
        var user = await ArrangeUserWithScope(db, DataScopeLevel.Department, "dashboard");
        db.Add(SupervisorRelation.Create(TenantId, SupervisorEmployeeId, SubordinateEmployeeId, DateTime.UtcNow.AddDays(-30)));
        await db.SaveChangesAsync();
        var resolver = CreateResolver(db);

        var widoczni = await resolver.GetVisibleEmployeeIdsAsync(
            user.Id, TenantId, SupervisorEmployeeId, "dashboard");

        Assert.NotNull(widoczni);
        Assert.Contains(SupervisorEmployeeId, widoczni!);
        Assert.Contains(SubordinateEmployeeId, widoczni!);
        Assert.DoesNotContain(StrangerEmployeeId, widoczni!);
    }

    [Fact]
    public async Task Zakres_calej_firmy_zwraca_null_czyli_brak_ograniczenia()
    {
        await using var db = CreateDbContext();
        var user = await ArrangeUserWithScope(db, DataScopeLevel.Organization, "dashboard");
        var resolver = CreateResolver(db);

        // null, a nie zbior wszystkich — materializowanie calej firmy tylko po to, zeby
        // niczego nie odfiltrowac, byloby marnotrawstwem przy kazdym wejsciu na pulpit.
        Assert.Null(await resolver.GetVisibleEmployeeIdsAsync(
            user.Id, TenantId, SupervisorEmployeeId, "dashboard"));
    }

    [Fact]
    public async Task Uzytkownik_bez_kartoteki_nie_widzi_nikogo_zamiast_wszystkich()
    {
        await using var db = CreateDbContext();
        var user = await ArrangeUserWithScope(db, DataScopeLevel.Department, "dashboard");
        var resolver = CreateResolver(db);

        // Konto bez powiazanej kartoteki pracownika. Pusty zbior oznacza „nic nie widzi";
        // gdyby zwrocic null, brak kartoteki otwieralby liczby calej firmy.
        var widoczni = await resolver.GetVisibleEmployeeIdsAsync(
            user.Id, TenantId, callerEmployeeId: null, "dashboard");

        Assert.NotNull(widoczni);
        Assert.Empty(widoczni!);
    }

    private static async Task<User> ArrangeUserWithScope(WorkBaseDbContext db, DataScopeLevel level, string module = "leave")
    {
        var user = User.Create($"user-{level}", $"{level}@example.com", "Test", "User", TenantId);
        var role = Role.Create(TenantId, $"Rola {level}", RoleType.Organizational, level: 10);
        db.AddRange(user, role);
        await db.SaveChangesAsync();
        db.Add(UserRole.Create(user.Id, role.Id, TenantId, "system"));
        db.Add(DataScope.Create(TenantId, role.Id, module, level));
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

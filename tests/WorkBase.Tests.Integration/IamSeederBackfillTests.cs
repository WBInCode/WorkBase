using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using WorkBase.Infrastructure.Persistence;
using WorkBase.Infrastructure.Seeding;
using WorkBase.Modules.Identity.Domain.Entities;
using Xunit;

namespace WorkBase.Tests.Integration;

/// <summary>
/// Uzupelnianie slownika uprawnien musi dzialac na bazie, ktorej numeracja nie jest ciagla.
/// </summary>
/// <remarks>
/// Pierwsza wersja tej funkcji wywalila start API na produkcji. Uprawnienia w seederze maja
/// identyfikatory wyliczane z kolejnego numeru (20000000-...-{n}), ale w dzialajacej instalacji
/// numeracja ma dziury i wartosci wstawione migracjami: config.manage siedzi pod numerem 100,
/// czyli takim, jaki wypadlby jednemu z dopisywanych uprawnien. EF probowal sledzic dwie encje
/// o tym samym kluczu i aplikacja nie wstawala.
/// </remarks>
public sealed class IamSeederBackfillTests
{
    private static readonly Guid TenantId = Guid.Parse("00000000-0000-0000-0000-000000000001");

    [Fact]
    public async Task Uzupelnianie_dziala_gdy_numeracja_uprawnien_ma_dziury()
    {
        await using var db = CreateDbContext();

        // Odtworzenie ukladu z produkcji: config.manage z numerem 100, ktory zderza sie
        // z numerem wypadajacym jednemu z dopisywanych uprawnien.
        var kolidujace = Permission.Create("config", "manage", null, "Zarzadzanie konfiguracja");
        db.Entry(kolidujace).Property(nameof(Permission.Id)).CurrentValue =
            Guid.Parse("20000000-0000-0000-0000-000000000100");
        db.Add(kolidujace);
        db.Add(Role.Create(TenantId, "Super Admin", RoleType.System, level: 0));
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();

        var wyjatek = await Record.ExceptionAsync(() => IamSeeder.SeedAsync(db, NullLogger.Instance));

        Assert.Null(wyjatek);
    }

    [Fact]
    public async Task Uzupelnianie_dopisuje_brakujace_uprawnienia_i_nadaje_je_rolom()
    {
        await using var db = CreateDbContext();
        var superAdmin = Role.Create(TenantId, "Super Admin", RoleType.System, level: 0);
        db.Add(superAdmin);
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();

        await IamSeeder.SeedAsync(db, NullLogger.Instance);

        var kody = await db.Set<Permission>().Select(p => p.Module + "." + p.Action).ToListAsync();
        Assert.Contains("ai.use", kody);
        Assert.Contains("forms.submit", kody);
        Assert.Contains("reports.manage", kody);

        var nadane = await db.Set<RolePermission>().CountAsync(rp => rp.RoleId == superAdmin.Id);
        Assert.Equal(IamSeeder.AllPermissionCodes.Count, nadane);
    }

    [Fact]
    public async Task Powtorne_uruchomienie_nie_dubluje_wpisow()
    {
        await using var db = CreateDbContext();
        db.Add(Role.Create(TenantId, "Super Admin", RoleType.System, level: 0));
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();

        await IamSeeder.SeedAsync(db, NullLogger.Instance);
        var poPierwszym = await db.Set<Permission>().CountAsync();
        var nadanePoPierwszym = await db.Set<RolePermission>().CountAsync();

        db.ChangeTracker.Clear();
        await IamSeeder.SeedAsync(db, NullLogger.Instance);

        Assert.Equal(poPierwszym, await db.Set<Permission>().CountAsync());
        Assert.Equal(nadanePoPierwszym, await db.Set<RolePermission>().CountAsync());
    }

    private static WorkBaseDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<WorkBaseDbContext>()
            .UseInMemoryDatabase($"iam-backfill-tests-{Guid.NewGuid():N}")
            .Options;
        return new WorkBaseDbContext(options);
    }
}

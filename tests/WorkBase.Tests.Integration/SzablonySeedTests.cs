using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using WorkBase.Infrastructure.Persistence;
using WorkBase.Infrastructure.Seeding;
using WorkBase.Modules.Notification.Domain.Entities;
using Xunit;

namespace WorkBase.Tests.Integration;

/// <summary>
/// Startowe szablony powiadomien i poprawka do tresci zasianych wczesniej.
/// </summary>
/// <remarks>
/// Produkcja pokazala luke, ktorej testy jednostkowe pokazac nie mogly: przypomnienie o terminie
/// idzie do pracownika I przelozonego, tekst awaryjny w kodzie zostal poprawiony o nazwisko —
/// ale AKTYWNY SZABLON przeslania tekst awaryjny, wiec piec firm zasianych wczesniej zostaloby
/// z wersja bez nazwiska na zawsze.
///
/// Poprawka dotyka wylacznie tresci identycznej ze starym ziarnem. Szablon, ktorego firma
/// dotknela, jest jej decyzja.
/// </remarks>
public class SzablonySeedTests
{
    private static readonly Guid Firma = Guid.Parse("80000000-0000-0000-0000-000000000001");

    [Fact]
    public async Task Nowa_firma_dostaje_szablon_dla_kazdego_kodu_ktory_wysylamy()
    {
        await using var db = UtworzBaze();

        await NotificationSeeder.SeedTenantAsync(db, Firma, NullLogger.Instance);

        var kody = await db.Set<NotificationTemplate>().IgnoreQueryFilters()
            .Select(t => t.Code).ToListAsync();
        Assert.Equal(
            new[] { "anomaly_detected", "escalation", "task_assigned", "task_overdue", "termin_minal", "termin_zbliza" },
            kody.OrderBy(k => k, StringComparer.Ordinal));
    }

    [Fact]
    public async Task Przypomnienie_o_terminie_mowi_czyj_to_termin()
    {
        await using var db = UtworzBaze();

        await NotificationSeeder.SeedTenantAsync(db, Firma, NullLogger.Instance);

        var szablon = await db.Set<NotificationTemplate>().IgnoreQueryFilters()
            .SingleAsync(t => t.Code == "termin_zbliza");
        Assert.Contains("{{pracownik}}", szablon.BodyTemplate);
    }

    [Fact]
    public async Task Stara_tresc_zasiana_wczesniej_zostaje_poprawiona()
    {
        await using var db = UtworzBaze();
        db.Add(NotificationTemplate.Create(
            Firma, "termin_zbliza", "Zbliżający się termin", "Termin się zbliża",
            "{{rodzaj}} — zostało {{dni}} dni ({{data}}).", "termin_zbliza"));
        await db.SaveChangesAsync();

        await NotificationSeeder.SeedTenantAsync(db, Firma, NullLogger.Instance);

        var szablon = await db.Set<NotificationTemplate>().IgnoreQueryFilters()
            .SingleAsync(t => t.Code == "termin_zbliza");
        Assert.Contains("{{pracownik}}", szablon.BodyTemplate);
    }

    [Fact]
    public async Task Szablon_zmieniony_przez_firme_zostaje_nietkniety()
    {
        await using var db = UtworzBaze();
        db.Add(NotificationTemplate.Create(
            Firma, "termin_zbliza", "Nasza nazwa", "Nasz tytul",
            "Uwaga, {{rodzaj}} konczy sie za {{dni}} dni.", "termin_zbliza"));
        await db.SaveChangesAsync();

        await NotificationSeeder.SeedTenantAsync(db, Firma, NullLogger.Instance);

        var szablon = await db.Set<NotificationTemplate>().IgnoreQueryFilters()
            .SingleAsync(t => t.Code == "termin_zbliza");
        Assert.Equal("Uwaga, {{rodzaj}} konczy sie za {{dni}} dni.", szablon.BodyTemplate);
        Assert.Equal("Nasza nazwa", szablon.Name);
    }

    /// <summary>Provisioning chodzi przy kazdej synchronizacji z Hubem, wiec musi byc idempotentny.</summary>
    [Fact]
    public async Task Powtorne_zasianie_nie_duplikuje_szablonow()
    {
        await using var db = UtworzBaze();

        await NotificationSeeder.SeedTenantAsync(db, Firma, NullLogger.Instance);
        await NotificationSeeder.SeedTenantAsync(db, Firma, NullLogger.Instance);

        Assert.Equal(6, await db.Set<NotificationTemplate>().IgnoreQueryFilters().CountAsync());
    }

    private static WorkBaseDbContext UtworzBaze() =>
        new(new DbContextOptionsBuilder<WorkBaseDbContext>()
            .UseInMemoryDatabase($"szablony-seed-{Guid.NewGuid():N}")
            .Options);
}

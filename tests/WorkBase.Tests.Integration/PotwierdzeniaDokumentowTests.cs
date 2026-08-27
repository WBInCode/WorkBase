using Microsoft.EntityFrameworkCore;
using WorkBase.Infrastructure.Dokumenty;
using WorkBase.Infrastructure.Persistence;
using WorkBase.Modules.Documents.Domain.Entities;
using WorkBase.Modules.Organization.Domain.Entities;
using Xunit;

namespace WorkBase.Tests.Integration;

/// <summary>
/// Potwierdzenie zapoznania sie z dokumentem.
/// </summary>
/// <remarks>
/// Wlasnosci, ktore latwo zgubic:
/// - dokument firmowy dotyczy kazdego AKTYWNEGO; przypiety do pracownika — tylko jego,
/// - potwierdzic moze wylacznie adresat; cudzy dokument daje te sama odpowiedz co nieistniejacy,
/// - potwierdzenie jest idempotentne,
/// - raport liczy zaleglosc od publikacji dokumentu, nie od „dzisiaj".
/// </remarks>
public class PotwierdzeniaDokumentowTests
{
    private static readonly Guid Firma = Guid.Parse("c0000000-0000-0000-0000-000000000001");

    [Fact]
    public async Task Dokument_firmowy_czeka_na_kazdego_aktywnego_ale_nie_na_zwolnionego()
    {
        await using var db = UtworzBaze();
        var (jan, anna, zwolniony) = await ZasiejPracownikow(db);
        var regulamin = await ZasiejDokument(db, wymaga: true);
        var serwis = new PotwierdzeniaDokumentow(db);

        Assert.Single(await serwis.DoPotwierdzeniaAsync(jan.Id, CancellationToken.None));
        Assert.Single(await serwis.DoPotwierdzeniaAsync(anna.Id, CancellationToken.None));

        var raport = await serwis.RaportAsync(regulamin.Id, CancellationToken.None);
        Assert.NotNull(raport);
        Assert.Equal(2, raport.Czeka);
        Assert.DoesNotContain(raport.Osoby, o => o.EmployeeId == zwolniony.Id);
    }

    [Fact]
    public async Task Dokument_przypiety_do_pracownika_dotyczy_tylko_jego()
    {
        await using var db = UtworzBaze();
        var (jan, anna, _) = await ZasiejPracownikow(db);
        var umowa = await ZasiejDokument(db, wymaga: true, entityType: "employee", entityId: jan.Id);
        var serwis = new PotwierdzeniaDokumentow(db);

        Assert.Single(await serwis.DoPotwierdzeniaAsync(jan.Id, CancellationToken.None));
        Assert.Empty(await serwis.DoPotwierdzeniaAsync(anna.Id, CancellationToken.None));

        // Anna nie jest adresatem: ta sama odpowiedz co dla nieistniejacego dokumentu.
        Assert.False(await serwis.PotwierdzAsync(umowa.Id, anna.Id, Firma, CancellationToken.None));
        Assert.True(await serwis.PotwierdzAsync(umowa.Id, jan.Id, Firma, CancellationToken.None));
    }

    [Fact]
    public async Task Potwierdzenie_znika_z_listy_i_jest_idempotentne()
    {
        await using var db = UtworzBaze();
        var (jan, _, _) = await ZasiejPracownikow(db);
        var regulamin = await ZasiejDokument(db, wymaga: true);
        var serwis = new PotwierdzeniaDokumentow(db);

        Assert.True(await serwis.PotwierdzAsync(regulamin.Id, jan.Id, Firma, CancellationToken.None));
        var pierwsze = (await db.Set<PotwierdzenieDokumentu>().SingleAsync()).PotwierdzonoDnia;

        Assert.True(await serwis.PotwierdzAsync(regulamin.Id, jan.Id, Firma, CancellationToken.None));

        Assert.Equal(1, await db.Set<PotwierdzenieDokumentu>().CountAsync());
        Assert.Equal(pierwsze, (await db.Set<PotwierdzenieDokumentu>().SingleAsync()).PotwierdzonoDnia);
        Assert.Empty(await serwis.DoPotwierdzeniaAsync(jan.Id, CancellationToken.None));
    }

    [Fact]
    public async Task Dokumentu_bez_flagi_nie_da_sie_potwierdzic()
    {
        await using var db = UtworzBaze();
        var (jan, _, _) = await ZasiejPracownikow(db);
        var zwykly = await ZasiejDokument(db, wymaga: false);
        var serwis = new PotwierdzeniaDokumentow(db);

        Assert.False(await serwis.PotwierdzAsync(zwykly.Id, jan.Id, Firma, CancellationToken.None));
        Assert.Empty(await serwis.DoPotwierdzeniaAsync(jan.Id, CancellationToken.None));
    }

    /// <summary>Zalacznik zadania nie ma adresata — nie ma kto potwierdzac.</summary>
    [Fact]
    public async Task Dokumentu_przypietego_do_zadania_nie_da_sie_oznaczyc()
    {
        await using var db = UtworzBaze();
        var zalacznik = await ZasiejDokument(db, wymaga: false, entityType: "task", entityId: Guid.NewGuid());
        var serwis = new PotwierdzeniaDokumentow(db);

        Assert.False(await serwis.UstawWymagaAsync(zalacznik.Id, true, CancellationToken.None));
        Assert.False(zalacznik.WymagaPotwierdzenia);
    }

    [Fact]
    public async Task Raport_mowi_kto_potwierdzil_i_od_ilu_dni_zalega_reszta()
    {
        await using var db = UtworzBaze();
        var (jan, anna, _) = await ZasiejPracownikow(db);
        var regulamin = await ZasiejDokument(db, wymaga: true);
        // Publikacja 10 dni temu — audyt ustawia CreatedAt przy zapisie, wiec cofamy wprost.
        db.Entry(regulamin).Property(d => d.CreatedAt).CurrentValue = DateTime.UtcNow.AddDays(-10);
        await db.SaveChangesAsync();
        var serwis = new PotwierdzeniaDokumentow(db);

        await serwis.PotwierdzAsync(regulamin.Id, jan.Id, Firma, CancellationToken.None);
        var raport = await serwis.RaportAsync(regulamin.Id, CancellationToken.None);

        Assert.NotNull(raport);
        Assert.Equal(1, raport.Potwierdzilo);
        Assert.Equal(1, raport.Czeka);
        var annaStan = Assert.Single(raport.Osoby, o => o.EmployeeId == anna.Id);
        Assert.Null(annaStan.PotwierdzonoDnia);
        Assert.Equal(10, annaStan.DniBezPotwierdzenia);
    }

    private static async Task<(Employee Jan, Employee Anna, Employee Zwolniony)> ZasiejPracownikow(WorkBaseDbContext db)
    {
        var jan = Employee.Create(Firma, "Jan", "Kowalski", "jan@example.com", null, DateTime.UtcNow.AddYears(-1));
        var anna = Employee.Create(Firma, "Anna", "Nowak", "anna@example.com", null, DateTime.UtcNow.AddYears(-1));
        var zwolniony = Employee.Create(Firma, "Piotr", "Byly", "piotr@example.com", null, DateTime.UtcNow.AddYears(-2));
        zwolniony.Deactivate(DateTime.UtcNow);
        db.AddRange(jan, anna, zwolniony);
        await db.SaveChangesAsync();
        return (jan, anna, zwolniony);
    }

    private static async Task<Document> ZasiejDokument(
        WorkBaseDbContext db, bool wymaga, string? entityType = null, Guid? entityId = null)
    {
        var dokument = Document.Create(
            Firma, "regulamin.pdf", "firma/regulamin.pdf", "application/pdf", 1024, Guid.NewGuid(),
            entityType: entityType, entityId: entityId);
        dokument.UstawWymagaPotwierdzenia(wymaga);
        db.Add(dokument);
        await db.SaveChangesAsync();
        return dokument;
    }

    private static WorkBaseDbContext UtworzBaze() =>
        new(new DbContextOptionsBuilder<WorkBaseDbContext>()
            .UseInMemoryDatabase($"potwierdzenia-{Guid.NewGuid():N}")
            .Options);
}

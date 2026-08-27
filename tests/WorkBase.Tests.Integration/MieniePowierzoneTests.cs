using Microsoft.EntityFrameworkCore;
using WorkBase.Infrastructure.Persistence;
using WorkBase.Modules.Organization.Application.Commands.Mienie;
using WorkBase.Modules.Organization.Domain.Entities;
using WorkBase.Modules.Organization.Infrastructure.Repositories;
using Xunit;

namespace WorkBase.Tests.Integration;

/// <summary>
/// Mienie powierzone: co firma wydala pracownikowi i co ma wrocic, gdy odchodzi.
/// </summary>
/// <remarks>
/// Trzy wlasnosci, ktore latwo zgubic przy refaktorze:
/// - potwierdzenie odbioru sklada WYLACZNIE wlasciciel wpisu (inaczej nic nie znaczy),
/// - zwrot nie kasuje wpisu (historia „kto mial ten laptop przede mna"),
/// - lista „do zwrotu" pokazuje tylko osoby, ktore odchodza — laptop u kogos, kto pracuje,
///   nie jest do zwrotu.
/// </remarks>
public class MieniePowierzoneTests
{
    private static readonly Guid Firma = Guid.Parse("b0000000-0000-0000-0000-000000000001");
    private static readonly DateOnly Dzis = new(2026, 8, 27);

    // --- domena ---

    [Fact]
    public void Zwrot_przed_wydaniem_jest_odrzucany()
    {
        var laptop = MieniePowierzone.Wydaj(Firma, Guid.NewGuid(), "Laptop", "ThinkPad T14", Dzis);

        Assert.Throws<ArgumentException>(() => laptop.Zwroc(Dzis.AddDays(-1), null));
    }

    [Fact]
    public void Drugi_zwrot_tej_samej_rzeczy_jest_odrzucany()
    {
        var laptop = MieniePowierzone.Wydaj(Firma, Guid.NewGuid(), "Laptop", "ThinkPad T14", Dzis);
        laptop.Zwroc(Dzis, null);

        Assert.Throws<InvalidOperationException>(() => laptop.Zwroc(Dzis, null));
    }

    /// <summary>Potwierdzenie ma date pierwszego klikniecia — ponowne nie moze jej przesunac.</summary>
    [Fact]
    public void Ponowne_potwierdzenie_nie_zmienia_daty()
    {
        var laptop = MieniePowierzone.Wydaj(Firma, Guid.NewGuid(), "Laptop", "ThinkPad T14", Dzis);
        var pierwsze = new DateTime(2026, 8, 27, 10, 0, 0, DateTimeKind.Utc);

        laptop.PotwierdzOdbior(pierwsze);
        laptop.PotwierdzOdbior(pierwsze.AddDays(3));

        Assert.Equal(pierwsze, laptop.PotwierdzonoOdbior);
    }

    // --- potwierdzenie: tylko wlasciciel ---

    [Fact]
    public async Task Pracownik_potwierdza_odbior_wlasnej_rzeczy()
    {
        await using var db = UtworzBaze();
        var (jan, _) = await ZasiejPracownikow(db);
        var laptop = MieniePowierzone.Wydaj(Firma, jan.Id, "Laptop", "ThinkPad T14", Dzis);
        db.Add(laptop);
        await db.SaveChangesAsync();

        var wynik = await new PotwierdzOdbiorMieniaHandler(new MieniePowierzoneRepository(db))
            .Handle(new PotwierdzOdbiorMieniaCommand(laptop.Id, jan.Id) { TenantId = Firma }, CancellationToken.None);

        Assert.True(wynik.IsSuccess);
        Assert.NotNull(laptop.PotwierdzonoOdbior);
    }

    /// <summary>
    /// Kadry moga wpisac wydanie, ale nie moga „potwierdzic za niego". Cudzy wpis dostaje
    /// NotFound, nie Forbidden — nie zdradzamy, ze taki identyfikator istnieje.
    /// </summary>
    [Fact]
    public async Task Cudzej_rzeczy_nie_da_sie_potwierdzic()
    {
        await using var db = UtworzBaze();
        var (jan, anna) = await ZasiejPracownikow(db);
        var laptop = MieniePowierzone.Wydaj(Firma, jan.Id, "Laptop", "ThinkPad T14", Dzis);
        db.Add(laptop);
        await db.SaveChangesAsync();

        var wynik = await new PotwierdzOdbiorMieniaHandler(new MieniePowierzoneRepository(db))
            .Handle(new PotwierdzOdbiorMieniaCommand(laptop.Id, anna.Id) { TenantId = Firma }, CancellationToken.None);

        Assert.True(wynik.IsFailure);
        Assert.Null(laptop.PotwierdzonoOdbior);
    }

    // --- lista „do zwrotu" ---

    [Fact]
    public async Task Do_zwrotu_trafia_tylko_niezwrocone_u_osob_ktore_odchodza()
    {
        await using var db = UtworzBaze();
        var (jan, anna) = await ZasiejPracownikow(db);

        // Jan odchodzi: ma laptop (niezwrocony) i telefon (zwrocony).
        jan.Deactivate(DateTime.UtcNow);
        var laptopJana = MieniePowierzone.Wydaj(Firma, jan.Id, "Laptop", "ThinkPad", Dzis.AddDays(-30));
        var telefonJana = MieniePowierzone.Wydaj(Firma, jan.Id, "Telefon", "Pixel", Dzis.AddDays(-30));
        telefonJana.Zwroc(Dzis, null);

        // Anna pracuje: jej laptop nie jest „do zwrotu".
        var laptopAnny = MieniePowierzone.Wydaj(Firma, anna.Id, "Laptop", "MacBook", Dzis.AddDays(-30));

        db.AddRange(laptopJana, telefonJana, laptopAnny);
        await db.SaveChangesAsync();

        var wynik = await new PobierzMienieDoZwrotuHandler(new MieniePowierzoneRepository(db))
            .Handle(new PobierzMienieDoZwrotuQuery { TenantId = Firma }, CancellationToken.None);

        var pozycja = Assert.Single(wynik.Value);
        Assert.Equal(laptopJana.Id, pozycja.Id);
        Assert.Equal("Jan Kowalski", pozycja.ImieNazwisko);
        Assert.Equal("nieaktywny", pozycja.Powod);
    }

    [Fact]
    public async Task Licznik_niezwroconych_liczy_tylko_niezwrocone()
    {
        await using var db = UtworzBaze();
        var (jan, _) = await ZasiejPracownikow(db);
        var laptop = MieniePowierzone.Wydaj(Firma, jan.Id, "Laptop", "ThinkPad", Dzis);
        var telefon = MieniePowierzone.Wydaj(Firma, jan.Id, "Telefon", "Pixel", Dzis);
        telefon.Zwroc(Dzis, null);
        db.AddRange(laptop, telefon);
        await db.SaveChangesAsync();

        var liczba = await new MieniePowierzoneRepository(db).PoliczNiezwroconeAsync(jan.Id);

        Assert.Equal(1, liczba);
    }

    private static async Task<(Employee Jan, Employee Anna)> ZasiejPracownikow(WorkBaseDbContext db)
    {
        var jan = Employee.Create(Firma, "Jan", "Kowalski", "jan@example.com", null, DateTime.UtcNow.AddYears(-1));
        var anna = Employee.Create(Firma, "Anna", "Nowak", "anna@example.com", null, DateTime.UtcNow.AddYears(-1));
        db.AddRange(jan, anna);
        await db.SaveChangesAsync();
        return (jan, anna);
    }

    private static WorkBaseDbContext UtworzBaze() =>
        new(new DbContextOptionsBuilder<WorkBaseDbContext>()
            .UseInMemoryDatabase($"mienie-{Guid.NewGuid():N}")
            .Options);
}

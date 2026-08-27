using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using WorkBase.Infrastructure.ListyKontrolne;
using WorkBase.Infrastructure.Persistence;
using WorkBase.Infrastructure.Seeding;
using WorkBase.Modules.Organization.Domain.Entities;
using WorkBase.Modules.Organization.Domain.Events;
using WorkBase.Modules.Tasks.Domain.Entities;
using Xunit;
using TaskStatus = WorkBase.Modules.Tasks.Domain.Entities.TaskStatus;

namespace WorkBase.Tests.Integration;

/// <summary>
/// Listy kontrolne przyjecia i pozegnania: szablon, ktory przy zdarzeniu sam zaklada zadania.
/// </summary>
/// <remarks>
/// Wlasnosci, ktore latwo zgubic:
/// - lista wylaczona nic nie robi (przyklady z ziarna sa wylaczone — firma nie moze dostac
///   zadan, o ktore nie prosila),
/// - pozycja bez wykonawcy (brak przelozonego) jest pomijana, reszta listy powstaje,
/// - termin = data zdarzenia + dni z pozycji,
/// - pozegnanie uruchamia tylko listy pozegnania.
/// </remarks>
public class ListyKontrolneTests
{
    private static readonly Guid Firma = Guid.Parse("d0000000-0000-0000-0000-000000000001");

    [Fact]
    public async Task Przyjecie_zaklada_zadania_z_aktywnej_listy_z_terminami_i_wykonawcami()
    {
        await using var db = UtworzBaze();
        var (nowy, szef, kadrowa) = await Zasiej(db);
        var lista = ListaKontrolna.Utworz(Firma, "Przyjęcie", WyzwalaczListy.Przyjecie, aktywna: true);
        lista.UstawPozycje(
        [
            ("Przygotuj stanowisko", 0, WykonawcaPozycji.Przelozony, null),
            ("Przeczytaj regulamin", 3, WykonawcaPozycji.Pracownik, null),
            ("Załóż teczkę", 1, WykonawcaPozycji.Osoba, kadrowa.Id),
        ]);
        db.Add(lista);
        await db.SaveChangesAsync();

        await Handler(db).Handle(new EmployeeCreatedEvent(nowy.Id, Firma), CancellationToken.None);

        var zadania = await db.Set<TaskItem>().OrderBy(t => t.DueDate).ToListAsync();
        Assert.Equal(3, zadania.Count);
        Assert.Equal(szef.Id, zadania.Single(t => t.Title == "Przygotuj stanowisko").AssigneeId);
        Assert.Equal(nowy.Id, zadania.Single(t => t.Title == "Przeczytaj regulamin").AssigneeId);
        Assert.Equal(kadrowa.Id, zadania.Single(t => t.Title == "Załóż teczkę").AssigneeId);
        Assert.Equal(DateTime.UtcNow.Date.AddDays(3), zadania.Single(t => t.Title == "Przeczytaj regulamin").DueDate);
        Assert.All(zadania, t => Assert.Contains("Jan Nowy", t.Description));
    }

    [Fact]
    public async Task Lista_wylaczona_nic_nie_zaklada()
    {
        await using var db = UtworzBaze();
        var (nowy, _, _) = await Zasiej(db);
        var lista = ListaKontrolna.Utworz(Firma, "Przyjęcie", WyzwalaczListy.Przyjecie, aktywna: false);
        lista.UstawPozycje([("Cokolwiek", 0, WykonawcaPozycji.Pracownik, null)]);
        db.Add(lista);
        await db.SaveChangesAsync();

        await Handler(db).Handle(new EmployeeCreatedEvent(nowy.Id, Firma), CancellationToken.None);

        Assert.Equal(0, await db.Set<TaskItem>().CountAsync());
    }

    /// <summary>Nowy pracownik bez przelozonego to normalna sytuacja tuz po dodaniu.</summary>
    [Fact]
    public async Task Pozycja_bez_wykonawcy_jest_pomijana_a_reszta_powstaje()
    {
        await using var db = UtworzBaze();
        var (nowy, _, _) = await Zasiej(db, zPrzelozonym: false);
        var lista = ListaKontrolna.Utworz(Firma, "Przyjęcie", WyzwalaczListy.Przyjecie, aktywna: true);
        lista.UstawPozycje(
        [
            ("Dla szefa", 0, WykonawcaPozycji.Przelozony, null),
            ("Dla nowego", 0, WykonawcaPozycji.Pracownik, null),
        ]);
        db.Add(lista);
        await db.SaveChangesAsync();

        await Handler(db).Handle(new EmployeeCreatedEvent(nowy.Id, Firma), CancellationToken.None);

        var zadanie = Assert.Single(await db.Set<TaskItem>().ToListAsync());
        Assert.Equal("Dla nowego", zadanie.Title);
    }

    [Fact]
    public async Task Pozegnanie_uruchamia_tylko_listy_pozegnania()
    {
        await using var db = UtworzBaze();
        var (odchodzacy, _, _) = await Zasiej(db);
        var przyjecie = ListaKontrolna.Utworz(Firma, "Przyjęcie", WyzwalaczListy.Przyjecie, aktywna: true);
        przyjecie.UstawPozycje([("Witaj", 0, WykonawcaPozycji.Pracownik, null)]);
        var pozegnanie = ListaKontrolna.Utworz(Firma, "Pożegnanie", WyzwalaczListy.Pozegnanie, aktywna: true);
        pozegnanie.UstawPozycje([("Odbierz laptop", 0, WykonawcaPozycji.Przelozony, null)]);
        db.AddRange(przyjecie, pozegnanie);
        await db.SaveChangesAsync();

        await Handler(db).Handle(new EmployeeDeactivatedEvent(odchodzacy.Id, Firma), CancellationToken.None);

        var zadanie = Assert.Single(await db.Set<TaskItem>().ToListAsync());
        Assert.Equal("Odbierz laptop", zadanie.Title);
    }

    [Fact]
    public async Task Ziarno_daje_dwa_wylaczone_przyklady_i_nie_dubluje()
    {
        await using var db = UtworzBaze();

        await ListyKontrolneSeeder.SeedTenantAsync(db, Firma, NullLogger.Instance);
        await ListyKontrolneSeeder.SeedTenantAsync(db, Firma, NullLogger.Instance);

        var listy = await db.Set<ListaKontrolna>().IgnoreQueryFilters().ToListAsync();
        Assert.Equal(2, listy.Count);
        Assert.All(listy, l => Assert.False(l.Aktywna));
        Assert.All(listy, l => Assert.NotEmpty(l.Pozycje));
    }

    [Fact]
    public void Pozycja_do_osoby_musi_ja_wskazywac()
    {
        var lista = ListaKontrolna.Utworz(Firma, "X", WyzwalaczListy.Przyjecie, aktywna: true);

        Assert.Throws<ArgumentException>(() =>
            lista.UstawPozycje([("Bez osoby", 0, WykonawcaPozycji.Osoba, null)]));
    }

    private static ZalozZadaniaZListyKontrolnej Handler(WorkBaseDbContext db) =>
        new(db, NullLogger<ZalozZadaniaZListyKontrolnej>.Instance);

    private static async Task<(Employee Nowy, Employee Szef, Employee Kadrowa)> Zasiej(
        WorkBaseDbContext db, bool zPrzelozonym = true)
    {
        var nowy = Employee.Create(Firma, "Jan", "Nowy", "jan@example.com", null, DateTime.UtcNow);
        var szef = Employee.Create(Firma, "Anna", "Szef", "anna@example.com", null, DateTime.UtcNow.AddYears(-3));
        var kadrowa = Employee.Create(Firma, "Ewa", "Kadry", "ewa@example.com", null, DateTime.UtcNow.AddYears(-3));
        db.AddRange(nowy, szef, kadrowa);
        db.Add(TaskStatus.Create(Firma, "NEW", "Nowe", isFinal: false, isDefault: true, color: null, sortOrder: 1));
        db.Add(TaskPriority.Create(Firma, "NORMAL", "Normalny", color: null, sortOrder: 2));
        if (zPrzelozonym)
            db.Add(SupervisorRelation.Create(Firma, supervisorEmployeeId: szef.Id, subordinateEmployeeId: nowy.Id, DateTime.UtcNow.AddDays(-1)));
        await db.SaveChangesAsync();
        return (nowy, szef, kadrowa);
    }

    private static WorkBaseDbContext UtworzBaze() =>
        new(new DbContextOptionsBuilder<WorkBaseDbContext>()
            .UseInMemoryDatabase($"listy-{Guid.NewGuid():N}")
            .Options);
}

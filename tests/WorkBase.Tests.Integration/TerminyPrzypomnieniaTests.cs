using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using WorkBase.Contracts;
using WorkBase.Infrastructure.Persistence;
using WorkBase.Infrastructure.Terminy;
using WorkBase.Modules.Organization.Domain.Entities;
using Xunit;
using Powiadomienie = WorkBase.Modules.Notification.Domain.Entities.Notification;

namespace WorkBase.Tests.Integration;

/// <summary>
/// Przypomnienia o terminach kadrowych.
/// </summary>
/// <remarks>
/// Najwazniejsza wlasnosc: termin odzywa sie DWA RAZY — gdy wchodzi w okno ostrzezenia i gdy
/// mija — a nie raz dziennie przez caly czas, gdy w tym oknie siedzi. Bez tego jedno badanie
/// lekarskie z miesiecznym wyprzedzeniem daloby trzydziesci powiadomien. Ta sama pulapka
/// wyszla wczesniej przy zaleglych zadaniach.
/// </remarks>
public class TerminyPrzypomnieniaTests
{
    private static readonly Guid Firma = Guid.Parse("50000000-0000-0000-0000-000000000001");
    private static readonly Guid Konto = Guid.Parse("50000000-0000-0000-0000-000000000002");

    [Fact]
    public async Task Termin_odlegly_nie_generuje_powiadomienia()
    {
        var (db, powiadomienia, job) = await Przygotuj(waznyZaDni: 90, dniOstrzezenia: 30);

        await job.ExecuteAsync();

        await powiadomienia.DidNotReceiveWithAnyArgs()
            .SendFromTemplateAsync(default, default, default!, default!, default!, default!, default!, default, default, default);
        await db.DisposeAsync();
    }

    [Fact]
    public async Task Termin_w_oknie_ostrzezenia_powiadamia_raz()
    {
        var (db, powiadomienia, job) = await Przygotuj(waznyZaDni: 10, dniOstrzezenia: 30);

        await job.ExecuteAsync();

        // Wartosci awaryjne to dawne teksty z kodu — sprawdzamy je, bo brak szablonu nie moze
        // zmienic tego, co dostanie uzytkownik.
        await powiadomienia.Received(1).SendFromTemplateAsync(
            Firma, Konto, "termin_zbliza", Arg.Any<IReadOnlyDictionary<string, string?>>(),
            "Termin się zbliża", Arg.Any<string>(),
            "termin_zbliza", "termin", Arg.Any<Guid>(), Arg.Any<CancellationToken>());
        await db.DisposeAsync();
    }

    [Fact]
    public async Task Kolejny_przebieg_nie_powtarza_tego_samego_ostrzezenia()
    {
        var (db, powiadomienia, job) = await Przygotuj(waznyZaDni: 10, dniOstrzezenia: 30);

        // Slad po wczorajszym przebiegu.
        var terminId = await db.Set<TerminPracownika>().IgnoreQueryFilters().Select(t => t.Id).FirstAsync();
        db.Set<Powiadomienie>().Add(Powiadomienie.Create(
            Firma, Konto, "Termin się zbliża", "cokolwiek", "termin_zbliza", "termin", terminId));
        await db.SaveChangesAsync();

        await job.ExecuteAsync();

        await powiadomienia.DidNotReceiveWithAnyArgs()
            .SendFromTemplateAsync(default, default, default!, default!, default!, default!, default!, default, default, default);
        await db.DisposeAsync();
    }

    /// <summary>
    /// Kluczowy przypadek: po ostrzezeniu termin MIJA i to jest nowa informacja, wiec musi
    /// pojawic sie drugie powiadomienie — innej kategorii.
    /// </summary>
    [Fact]
    public async Task Po_uplywie_terminu_idzie_drugie_powiadomienie_mimo_wczesniejszego_ostrzezenia()
    {
        var (db, powiadomienia, job) = await Przygotuj(waznyZaDni: -3, dniOstrzezenia: 30);

        var terminId = await db.Set<TerminPracownika>().IgnoreQueryFilters().Select(t => t.Id).FirstAsync();
        db.Set<Powiadomienie>().Add(Powiadomienie.Create(
            Firma, Konto, "Termin się zbliża", "cokolwiek", "termin_zbliza", "termin", terminId));
        await db.SaveChangesAsync();

        await job.ExecuteAsync();

        await powiadomienia.Received(1).SendFromTemplateAsync(
            Firma, Konto, "termin_minal", Arg.Any<IReadOnlyDictionary<string, string?>>(),
            "Termin minął", Arg.Any<string>(),
            "termin_minal", "termin", terminId, Arg.Any<CancellationToken>());
        await db.DisposeAsync();
    }

    [Fact]
    public async Task Termin_zarchiwizowany_jest_pomijany()
    {
        var (db, powiadomienia, job) = await Przygotuj(waznyZaDni: -3, dniOstrzezenia: 30);

        var termin = await db.Set<TerminPracownika>().IgnoreQueryFilters().FirstAsync();
        termin.Zarchiwizuj();
        await db.SaveChangesAsync();

        await job.ExecuteAsync();

        await powiadomienia.DidNotReceiveWithAnyArgs()
            .SendFromTemplateAsync(default, default, default!, default!, default!, default!, default!, default, default, default);
        await db.DisposeAsync();
    }

    private static async Task<(WorkBaseDbContext, INotificationService, TerminyPrzypomnieniaJob)> Przygotuj(
        int waznyZaDni, int dniOstrzezenia)
    {
        var db = new WorkBaseDbContext(new DbContextOptionsBuilder<WorkBaseDbContext>()
            .UseInMemoryDatabase($"terminy-{Guid.NewGuid():N}")
            .Options);

        var pracownik = Employee.Create(Firma, "Jan", "Kowalski", "jan@example.com", null, DateTime.UtcNow.AddYears(-1));
        pracownik.LinkUser(Konto);
        db.Add(pracownik);

        var typ = TypTerminu.Utworz(Firma, "BADANIA", "Badania lekarskie", null, dniOstrzezenia);
        db.Add(typ);
        await db.SaveChangesAsync();

        db.Add(TerminPracownika.Utworz(
            Firma, pracownik.Id, typ.Id,
            DateOnly.FromDateTime(DateTime.UtcNow).AddDays(waznyZaDni)));
        await db.SaveChangesAsync();

        var powiadomienia = Substitute.For<INotificationService>();
        var job = new TerminyPrzypomnieniaJob(
            db, powiadomienia, NullLogger<TerminyPrzypomnieniaJob>.Instance);

        return (db, powiadomienia, job);
    }
}

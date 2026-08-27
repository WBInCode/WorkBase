using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using WorkBase.Contracts;
using WorkBase.Infrastructure.Persistence;
using WorkBase.Modules.Notification.Domain.Entities;
using WorkBase.Modules.Notification.Infrastructure.Hubs;
using WorkBase.Modules.Notification.Infrastructure.Services;
using Xunit;

namespace WorkBase.Tests.Integration;

/// <summary>
/// Szablony powiadomien — tresc, ktora firma moze zmienic pod siebie.
/// </summary>
/// <remarks>
/// Ekran szablonow istnial od dawna, mial pelny CRUD i zapisywal dane, ale NIKT NIGDY NIE
/// RENDEROWAL SZABLONU: wszyscy wolajacy sklejali teksty na sztywno w kodzie. Administrator
/// konfigurowal i nic sie nie dzialo.
///
/// Najwazniejsza wlasnosc tych testow: brak szablonu, szablon wylaczony ani literowka w nim
/// NIE MOGA uciszyc powiadomienia. Najgorsze, co wolno sie stac, to powrot do tresci domyslnej.
/// </remarks>
public class SzablonyPowiadomienTests
{
    private static readonly Guid Firma = Guid.Parse("60000000-0000-0000-0000-000000000001");
    private static readonly Guid Konto = Guid.Parse("60000000-0000-0000-0000-000000000002");

    [Fact]
    public async Task Bez_szablonu_uzywamy_tresci_domyslnej()
    {
        await using var db = UtworzBaze();
        var serwis = Zbuduj(db);

        await serwis.SendFromTemplateAsync(
            Firma, Konto, "task_overdue",
            new Dictionary<string, string?> { ["tytul"] = "Faktura" },
            "Zadanie po terminie", "Faktura — termin minal", "task_overdue");

        var wyslane = await db.Set<Notification>().IgnoreQueryFilters().SingleAsync();
        Assert.Equal("Zadanie po terminie", wyslane.Title);
        Assert.Equal("Faktura — termin minal", wyslane.Body);
    }

    [Fact]
    public async Task Szablon_firmy_zastepuje_tresc_domyslna_i_podstawia_zmienne()
    {
        await using var db = UtworzBaze();
        db.Add(NotificationTemplate.Create(
            Firma, "task_overdue", "Po terminie",
            "Pilne: {{tytul}}", "Zadanie {{tytul}} czeka od {{dni}} dni.", "task_overdue"));
        await db.SaveChangesAsync();
        var serwis = Zbuduj(db);

        await serwis.SendFromTemplateAsync(
            Firma, Konto, "task_overdue",
            new Dictionary<string, string?> { ["tytul"] = "Faktura", ["dni"] = "3" },
            "domyslny tytul", "domyslna tresc", "task_overdue");

        var wyslane = await db.Set<Notification>().IgnoreQueryFilters().SingleAsync();
        Assert.Equal("Pilne: Faktura", wyslane.Title);
        Assert.Equal("Zadanie Faktura czeka od 3 dni.", wyslane.Body);
    }

    /// <summary>
    /// Wylaczenie szablonu ma przywracac tresc domyslna, a nie uciszac powiadomienie.
    /// </summary>
    [Fact]
    public async Task Szablon_wylaczony_wraca_do_tresci_domyslnej()
    {
        await using var db = UtworzBaze();
        var szablon = NotificationTemplate.Create(
            Firma, "task_overdue", "Po terminie", "Nieuzywany", "Nieuzywana", "task_overdue");
        szablon.Deactivate();
        db.Add(szablon);
        await db.SaveChangesAsync();
        var serwis = Zbuduj(db);

        await serwis.SendFromTemplateAsync(
            Firma, Konto, "task_overdue", new Dictionary<string, string?>(),
            "Zadanie po terminie", "tresc domyslna", "task_overdue");

        var wyslane = await db.Set<Notification>().IgnoreQueryFilters().SingleAsync();
        Assert.Equal("Zadanie po terminie", wyslane.Title);
    }

    /// <summary>
    /// Literowka w znaczniku ma zostac widoczna. Podstawienie pustki zamienialoby blad autora
    /// szablonu w zdanie z dziura, ktorej nie da sie z niczym powiazac.
    /// </summary>
    [Fact]
    public async Task Nieznany_znacznik_zostaje_w_tekscie()
    {
        await using var db = UtworzBaze();
        db.Add(NotificationTemplate.Create(
            Firma, "test", "Test", "Tytul", "Masz {{literowka}} do zrobienia.", "test"));
        await db.SaveChangesAsync();
        var serwis = Zbuduj(db);

        await serwis.SendFromTemplateAsync(
            Firma, Konto, "test",
            new Dictionary<string, string?> { ["tytul"] = "cokolwiek" },
            "domyslny", "domyslna", "test");

        var wyslane = await db.Set<Notification>().IgnoreQueryFilters().SingleAsync();
        Assert.Equal("Masz {{literowka}} do zrobienia.", wyslane.Body);
    }

    /// <summary>
    /// Pole zostawione puste to co innego niz znacznik, ktorego nie znamy — puste podstawiamy.
    /// </summary>
    [Fact]
    public async Task Zmienna_o_wartosci_null_daje_pusty_tekst()
    {
        await using var db = UtworzBaze();
        db.Add(NotificationTemplate.Create(
            Firma, "test", "Test", "Tytul", "Uwagi: {{uwagi}}.", "test"));
        await db.SaveChangesAsync();
        var serwis = Zbuduj(db);

        await serwis.SendFromTemplateAsync(
            Firma, Konto, "test",
            new Dictionary<string, string?> { ["uwagi"] = null },
            "domyslny", "domyslna", "test");

        var wyslane = await db.Set<Notification>().IgnoreQueryFilters().SingleAsync();
        Assert.Equal("Uwagi: .", wyslane.Body);
    }

    [Fact]
    public async Task Szablon_innej_firmy_nas_nie_dotyczy()
    {
        await using var db = UtworzBaze();
        db.Add(NotificationTemplate.Create(
            Guid.NewGuid(), "task_overdue", "Cudzy", "Cudzy tytul", "Cudza tresc", "task_overdue"));
        await db.SaveChangesAsync();
        var serwis = Zbuduj(db);

        await serwis.SendFromTemplateAsync(
            Firma, Konto, "task_overdue", new Dictionary<string, string?>(),
            "Zadanie po terminie", "tresc domyslna", "task_overdue");

        var wyslane = await db.Set<Notification>().IgnoreQueryFilters()
            .SingleAsync(n => n.TenantId == Firma);
        Assert.Equal("Zadanie po terminie", wyslane.Title);
    }

    // --- preferencje odbiorcy ---

    /// <summary>
    /// Brak wiersza preferencji ma znaczyc „wysylaj". Odwrotna domyslka uciszylaby powiadomienia
    /// wszystkim, ktorzy nigdy nic nie ustawili — czyli wszystkim.
    /// </summary>
    [Fact]
    public async Task Bez_ustawionych_preferencji_powiadomienie_dochodzi()
    {
        await using var db = UtworzBaze();
        var serwis = Zbuduj(db);

        await serwis.SendAsync(Firma, Konto, "Tytul", "Tresc", "task_assigned");

        Assert.Equal(1, await db.Set<Notification>().IgnoreQueryFilters().CountAsync());
    }

    [Fact]
    public async Task Wylaczona_kategoria_wycisza_powiadomienie()
    {
        await using var db = UtworzBaze();
        db.Add(NotificationPreference.Create(Firma, Konto, "task_assigned", inApp: false));
        await db.SaveChangesAsync();
        var serwis = Zbuduj(db);

        await serwis.SendAsync(Firma, Konto, "Tytul", "Tresc", "task_assigned");

        Assert.Equal(0, await db.Set<Notification>().IgnoreQueryFilters().CountAsync());
    }

    /// <summary>
    /// Wyciszenie jednej kategorii nie moze wyciszyc pozostalych — inaczej rezygnacja
    /// z powiadomien o zadaniach zabralaby tez informacje o decyzji w sprawie wniosku.
    /// </summary>
    [Fact]
    public async Task Wylaczenie_jednej_kategorii_nie_dotyka_innych()
    {
        await using var db = UtworzBaze();
        db.Add(NotificationPreference.Create(Firma, Konto, "task_assigned", inApp: false));
        await db.SaveChangesAsync();
        var serwis = Zbuduj(db);

        await serwis.SendAsync(Firma, Konto, "Tytul", "Tresc", "termin_minal");

        Assert.Equal(1, await db.Set<Notification>().IgnoreQueryFilters().CountAsync());
    }

    [Fact]
    public async Task Preferencja_innej_osoby_nas_nie_wycisza()
    {
        await using var db = UtworzBaze();
        db.Add(NotificationPreference.Create(Firma, Guid.NewGuid(), "task_assigned", inApp: false));
        await db.SaveChangesAsync();
        var serwis = Zbuduj(db);

        await serwis.SendAsync(Firma, Konto, "Tytul", "Tresc", "task_assigned");

        Assert.Equal(1, await db.Set<Notification>().IgnoreQueryFilters().CountAsync());
    }

    // --- kanal pocztowy ---

    /// <summary>
    /// Domyslka pocztowa jest ODWROTNA niz w aplikacji. Poczta wychodzi poza system, do skrzynki,
    /// ktorej nikt o zgode nie pytal — wlaczenie musi byc swiadome.
    /// </summary>
    [Fact]
    public async Task Bez_ustawionych_preferencji_mail_NIE_wychodzi()
    {
        await using var db = UtworzBaze();
        var (serwis, poczta, _) = ZbudujZPoczta(db);

        await serwis.SendAsync(Firma, Konto, "Tytul", "Tresc", "task_assigned");

        await poczta.DidNotReceiveWithAnyArgs().SendAsync(default!, default!, default!, default);
    }

    [Fact]
    public async Task Wlaczony_kanal_pocztowy_wysyla_na_adres_z_kartoteki()
    {
        await using var db = UtworzBaze();
        db.Add(NotificationPreference.Create(Firma, Konto, "task_assigned", inApp: true, email: true));
        await db.SaveChangesAsync();
        var (serwis, poczta, _) = ZbudujZPoczta(db, adres: "anna@example.com");

        await serwis.SendAsync(Firma, Konto, "Zadanie", "Rozliczyc fakture", "task_assigned");

        await poczta.Received(1).SendAsync(
            "anna@example.com", "Zadanie", Arg.Is<string>(t => t.Contains("Rozliczyc fakture")),
            Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Tresc niesie dane od uzytkownikow (tytul zadania, nazwisko). Bez ucieczki nawias ostry
    /// rozjechalby wiadomosc, a w gorszym wariancie wstrzyknal do niej znaczniki.
    /// </summary>
    [Fact]
    public async Task Tresc_trafia_do_maila_po_ucieczce_html()
    {
        await using var db = UtworzBaze();
        db.Add(NotificationPreference.Create(Firma, Konto, "task_assigned", inApp: true, email: true));
        await db.SaveChangesAsync();
        var (serwis, poczta, _) = ZbudujZPoczta(db, adres: "anna@example.com");

        await serwis.SendAsync(Firma, Konto, "Tytul", "<script>alert(1)</script>", "task_assigned");

        await poczta.Received(1).SendAsync(
            Arg.Any<string>(), Arg.Any<string>(),
            Arg.Is<string>(t => !t.Contains("<script>") && t.Contains("&lt;script&gt;")),
            Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Wyciszenie w aplikacji ucisza rowniez poczte. Inaczej „nie chce tego widziec" konczyloby sie
    /// tym samym powiadomieniem w skrzynce.
    /// </summary>
    [Fact]
    public async Task Wyciszenie_w_aplikacji_zabiera_takze_mail()
    {
        await using var db = UtworzBaze();
        db.Add(NotificationPreference.Create(Firma, Konto, "task_assigned", inApp: false, email: true));
        await db.SaveChangesAsync();
        var (serwis, poczta, _) = ZbudujZPoczta(db, adres: "anna@example.com");

        await serwis.SendAsync(Firma, Konto, "Tytul", "Tresc", "task_assigned");

        await poczta.DidNotReceiveWithAnyArgs().SendAsync(default!, default!, default!, default);
    }

    /// <summary>
    /// Awaria poczty NIE MOZE zabrac powiadomienia w aplikacji ani przerwac przebiegu zadania
    /// cyklicznego — pozostali odbiorcy z tej samej petli musza dostac swoje.
    /// </summary>
    [Fact]
    public async Task Awaria_smtp_nie_zabiera_powiadomienia_w_aplikacji()
    {
        await using var db = UtworzBaze();
        db.Add(NotificationPreference.Create(Firma, Konto, "task_assigned", inApp: true, email: true));
        await db.SaveChangesAsync();
        var (serwis, poczta, _) = ZbudujZPoczta(db, adres: "anna@example.com");
        poczta.SendAsync(default!, default!, default!, default)
            .ReturnsForAnyArgs(Task.FromException(new InvalidOperationException("SMTP lezy")));

        await serwis.SendAsync(Firma, Konto, "Tytul", "Tresc", "task_assigned");

        Assert.Equal(1, await db.Set<Notification>().IgnoreQueryFilters().CountAsync());
    }

    /// <summary>Konto bez kartoteki nie ma adresu — nie ma dokad wyslac.</summary>
    [Fact]
    public async Task Konto_bez_adresu_nie_generuje_maila()
    {
        await using var db = UtworzBaze();
        db.Add(NotificationPreference.Create(Firma, Konto, "task_assigned", inApp: true, email: true));
        await db.SaveChangesAsync();
        var (serwis, poczta, _) = ZbudujZPoczta(db, adres: null);

        await serwis.SendAsync(Firma, Konto, "Tytul", "Tresc", "task_assigned");

        await poczta.DidNotReceiveWithAnyArgs().SendAsync(default!, default!, default!, default);
    }

    private static NotificationService Zbuduj(WorkBaseDbContext db)
    {
        var hub = Substitute.For<IHubContext<NotificationHub>>();
        hub.Clients.Group(Arg.Any<string>()).Returns(Substitute.For<IClientProxy>());

        return new NotificationService(
            db, hub,
            Substitute.For<IHubNotificationForwarder>(),
            Substitute.For<IChatNoticeForwarder>());
    }

    private static (NotificationService, IEmailSender, IOrganizationLookupService) ZbudujZPoczta(
        WorkBaseDbContext db, string? adres = null)
    {
        var hub = Substitute.For<IHubContext<NotificationHub>>();
        hub.Clients.Group(Arg.Any<string>()).Returns(Substitute.For<IClientProxy>());

        var poczta = Substitute.For<IEmailSender>();
        var organizacja = Substitute.For<IOrganizationLookupService>();
        organizacja.GetEmailByUserIdAsync(Firma, Konto, Arg.Any<CancellationToken>()).Returns(adres);

        var serwis = new NotificationService(
            db, hub,
            Substitute.For<IHubNotificationForwarder>(),
            Substitute.For<IChatNoticeForwarder>(),
            poczta, organizacja);

        return (serwis, poczta, organizacja);
    }

    private static WorkBaseDbContext UtworzBaze()
    {
        var options = new DbContextOptionsBuilder<WorkBaseDbContext>()
            .UseInMemoryDatabase($"szablony-{Guid.NewGuid():N}")
            .Options;
        return new WorkBaseDbContext(options);
    }
}

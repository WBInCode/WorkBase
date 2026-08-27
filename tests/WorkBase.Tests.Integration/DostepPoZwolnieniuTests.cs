using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using WorkBase.Contracts;
using WorkBase.Modules.Organization.Application.Contracts;
using WorkBase.Modules.Organization.Application.EventHandlers;
using WorkBase.Modules.Organization.Domain.Entities;
using WorkBase.Modules.Organization.Domain.Events;
using Xunit;

namespace WorkBase.Tests.Integration;

/// <summary>
/// Zwolnienie pracownika odbiera mu dostep; przywrocenie oddaje.
/// </summary>
/// <remarks>
/// Istnial ProvisionKeycloakUserOnEmployeeCreated, ale NIE ISTNIAL handler lustrzany:
/// EmployeeDeactivatedEvent bylo podnoszone i nikt go nie obslugiwal. Zwolniony pracownik
/// zachowywal konto i mogl sie logowac dalej, a kadry widzialy status „Nieaktywny" i mialy
/// prawo sadzic, ze dostep zniknal.
/// </remarks>
public class DostepPoZwolnieniuTests
{
    private static readonly Guid Firma = Guid.Parse("90000000-0000-0000-0000-000000000001");
    private static readonly Guid Konto = Guid.Parse("90000000-0000-0000-0000-000000000002");

    [Fact]
    public async Task Zwolnienie_wylacza_konto()
    {
        var (handler, keycloak, pracownik) = Przygotuj();

        await handler.Handle(new EmployeeDeactivatedEvent(pracownik.Id, Firma), CancellationToken.None);

        await keycloak.Received(1).SetUserEnabledAsync(
            Arg.Any<string?>(), Konto.ToString(), false, Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Samo wylaczenie nie wystarcza: konto wylaczone w trakcie sesji ma wazny token az do jego
    /// wygasniecia, wiec zwolniony pracowalby dalej.
    /// </summary>
    [Fact]
    public async Task Zwolnienie_zamyka_takze_otwarte_sesje()
    {
        var (handler, keycloak, pracownik) = Przygotuj();

        await handler.Handle(new EmployeeDeactivatedEvent(pracownik.Id, Firma), CancellationToken.None);

        await keycloak.Received(1).LogoutUserSessionsAsync(
            Arg.Any<string?>(), "anna@example.com", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Przywrocenie_wlacza_konto_i_nie_zamyka_sesji()
    {
        var (handler, keycloak, pracownik) = Przygotuj();

        await handler.Handle(new EmployeeActivatedEvent(pracownik.Id, Firma), CancellationToken.None);

        await keycloak.Received(1).SetUserEnabledAsync(
            Arg.Any<string?>(), Konto.ToString(), true, Arg.Any<CancellationToken>());
        await keycloak.DidNotReceiveWithAnyArgs().LogoutUserSessionsAsync(default, default!, default);
    }

    /// <summary>Firma z wlasnym realmem — konto trzeba wylaczyc tam, gdzie ono jest.</summary>
    [Fact]
    public async Task Konto_wylaczamy_w_realmie_firmy()
    {
        var (handler, keycloak, pracownik) = Przygotuj(realm: "firma-abc");

        await handler.Handle(new EmployeeDeactivatedEvent(pracownik.Id, Firma), CancellationToken.None);

        await keycloak.Received(1).SetUserEnabledAsync(
            "firma-abc", Arg.Any<string>(), false, Arg.Any<CancellationToken>());
    }

    /// <summary>Praca przy terminalu bez logowania to normalna sytuacja — nie ma czego odbierac.</summary>
    [Fact]
    public async Task Pracownik_bez_konta_nie_wola_keycloaka()
    {
        var (handler, keycloak, pracownik) = Przygotuj(zKontem: false);

        await handler.Handle(new EmployeeDeactivatedEvent(pracownik.Id, Firma), CancellationToken.None);

        await keycloak.DidNotReceiveWithAnyArgs().SetUserEnabledAsync(default, default!, default, default);
    }

    /// <summary>
    /// Awaria Keycloaka nie moze wycofac zwolnienia — zmiana w kadrach jest juz zapisana,
    /// a handler chodzi PO zatwierdzeniu transakcji.
    /// </summary>
    [Fact]
    public async Task Awaria_keycloaka_nie_przerywa_zwolnienia()
    {
        var (handler, keycloak, pracownik) = Przygotuj();
        keycloak.SetUserEnabledAsync(default, default!, default, default)
            .ThrowsForAnyArgs(new HttpRequestException("Keycloak nie odpowiada"));

        await handler.Handle(new EmployeeDeactivatedEvent(pracownik.Id, Firma), CancellationToken.None);
    }

    /// <summary>Nieudane wylaczenie nie moze udawac zamknietego tematu — sesji wtedy nie zamykamy.</summary>
    [Fact]
    public async Task Nieudane_wylaczenie_nie_konczy_sie_zamknieciem_sesji()
    {
        var (handler, keycloak, pracownik) = Przygotuj();
        keycloak.SetUserEnabledAsync(default, default!, default, default).ReturnsForAnyArgs(false);

        await handler.Handle(new EmployeeDeactivatedEvent(pracownik.Id, Firma), CancellationToken.None);

        await keycloak.DidNotReceiveWithAnyArgs().LogoutUserSessionsAsync(default, default!, default);
    }

    private static (DostepKeycloakPoZmianieStatusu, IKeycloakAdminService, Employee) Przygotuj(
        bool zKontem = true, string? realm = null)
    {
        var pracownik = Employee.Create(
            Firma, "Anna", "Nowak", "anna@example.com", null, DateTime.UtcNow.AddYears(-2));
        if (zKontem) pracownik.LinkUser(Konto);

        var firma = Tenant.Create("Testowa", "testowa");
        if (realm is not null) firma.AssignKeycloakRealm(realm);

        var pracownicy = Substitute.For<IEmployeeRepository>();
        pracownicy.GetByIdAsync(pracownik.Id, Arg.Any<CancellationToken>()).Returns(pracownik);

        var firmy = Substitute.For<ITenantRepository>();
        firmy.GetByIdAsync(Firma, Arg.Any<CancellationToken>()).Returns(firma);

        var keycloak = Substitute.For<IKeycloakAdminService>();
        keycloak.SetUserEnabledAsync(default, default!, default, default).ReturnsForAnyArgs(true);

        var handler = new DostepKeycloakPoZmianieStatusu(
            pracownicy, firmy, keycloak, NullLogger<DostepKeycloakPoZmianieStatusu>.Instance);

        return (handler, keycloak, pracownik);
    }
}

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using WorkBase.Contracts;
using WorkBase.Infrastructure.Auth.MultiRealm;
using WorkBase.Infrastructure.Persistence;
using WorkBase.Infrastructure.Services;
using WorkBase.Infrastructure.Setup;
using Xunit;

namespace WorkBase.Tests.Integration;

/// <summary>
/// Dwie wlasnosci, na ktorych stoi bezpieczenstwo bramki SETUP_REQUIRED — obie byly opisane
/// w komentarzach i nietestowane.
///
/// Pierwsza: nowa firma MUSI dostac znacznik, inaczej kreator nigdy sie nie pokaze i wlasciciel
/// wyladuje w pustej aplikacji bez typow urlopu i przelozonych.
///
/// Druga, wazniejsza: ponowna synchronizacja z Hubem NIE MOZE oznaczyc firmy juz dzialajacej.
/// HubEntitlementsSyncService.SyncAllAsync przechodzi po wszystkich firmach przy kazdym starcie
/// aplikacji — gdyby trafiala w te sama galaz co tworzenie, pierwsze wdrozenie zablokowaloby
/// wszystkich dotychczasowych klientow naraz. Test wywoluje prawdziwy serwis, a nie sam
/// IKonfiguracjaStartowaService, wlasnie po to, zeby usuniecie wywolania w kodzie oblalo test.
/// </summary>
public class ProvisioningZnacznikKreatoraTests
{
    [Fact]
    public async Task Nowa_firma_dostaje_znacznik_konfiguracji()
    {
        var (serwis, konfiguracja) = Zbuduj();

        var wynik = await serwis.EnsureHubTenantAsync(Rejestracja());

        Assert.True(wynik.Created);
        await konfiguracja.Received(1).OznaczJakoWymaganaAsync(wynik.TenantId, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Ponowna_synchronizacja_nie_blokuje_dzialajacej_firmy()
    {
        var (serwis, konfiguracja) = Zbuduj();

        var pierwsze = await serwis.EnsureHubTenantAsync(Rejestracja());
        konfiguracja.ClearReceivedCalls();

        // Dokladnie to robi HubEntitlementsSyncService przy kazdym starcie aplikacji.
        var drugie = await serwis.EnsureHubTenantAsync(Rejestracja());

        Assert.False(drugie.Created);
        Assert.Equal(pierwsze.TenantId, drugie.TenantId);
        await konfiguracja.DidNotReceiveWithAnyArgs()
            .OznaczJakoWymaganaAsync(default, default);
    }

    private static HubTenantRegistration Rejestracja() =>
        new("org-hub-1", "instancja-workbase-1", "Firma Testowa sp. z o.o.", "firma-testowa");

    private static (TenantProvisioningService, IKonfiguracjaStartowaService) Zbuduj()
    {
        var db = new WorkBaseDbContext(new DbContextOptionsBuilder<WorkBaseDbContext>()
            .UseInMemoryDatabase($"provisioning-{Guid.NewGuid():N}")
            .Options);

        var konfiguracja = Substitute.For<IKonfiguracjaStartowaService>();

        var ustawienia = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Keycloak:Authority"] = "https://auth.example.test/realms/workbase",
            })
            .Build();

        var serwis = new TenantProvisioningService(
            db,
            Substitute.For<IKeycloakAdminService>(),
            Substitute.For<IKioskAccountProvisioningService>(),
            new TenantIssuerCache(ustawienia),
            ustawienia,
            konfiguracja,
            NullLogger<TenantProvisioningService>.Instance);

        return (serwis, konfiguracja);
    }
}

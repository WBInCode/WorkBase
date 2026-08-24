using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using WorkBase.Infrastructure.Setup;
using Xunit;

namespace WorkBase.Tests.Integration;

/// <summary>
/// Blokada „firma nie ukonczyla konfiguracji" — i dowod, ze nie zamyka drzwi.
/// </summary>
/// <remarks>
/// To jest najniebezpieczniejszy element kreatora: zle dobrana lista wyjatkow potrafi odciac
/// logowanie albo webhooki z Huba i firma nie wejdzie do systemu w ogole. Dlatego bialej
/// listy pilnuja tu testy, a nie sama uwaga przy przegladzie kodu.
///
/// Druga wlasnosc, rownie wazna: firmy zalozone PRZED powstaniem kreatora nie maja znacznika
/// setup.required i blokada nie moze ich dotknac. Na produkcji dziala na tym WB Partners.
/// </remarks>
[Collection("Integration")]
public class KonfiguracjaStartowaTests
{
    private readonly WorkBaseWebFactory _factory;

    public KonfiguracjaStartowaTests(WorkBaseWebFactory factory) => _factory = factory;

    /// <summary>
    /// Trasy, ktorych zablokowanie odcina wejscie do systemu albo uniemozliwia ukonczenie
    /// samego kreatora. Kazda musi przejsc przez bialą listę.
    /// </summary>
    [Theory]
    [InlineData("/api/setup/state", "kreator nie dalby sie ukonczyc")]
    [InlineData("/api/setup/complete", "kreator nie dalby sie ukonczyc")]
    [InlineData("/api/auth/me", "interfejs nie odczytalby uprawnien i firmy")]
    [InlineData("/api/hub/sso/callback", "logowanie z Huba przestaloby dzialac")]
    [InlineData("/api/hub/sso/logout", "wylogowanie z Huba przestaloby dzialac")]
    [InlineData("/sso/callback", "starszy adres logowania z Huba")]
    [InlineData("/api/onboarding/status", "rejestracja samoobslugowa dziala bez firmy")]
    [InlineData("/api/billing/webhook", "webhook Stripe'a przychodzi bez kontekstu firmy")]
    [InlineData("/health", "kontrola zdrowia w potoku wdrozeniowym")]
    [InlineData("/hubs/notifications", "SignalR")]
    [InlineData("/", "strona glowna API")]
    public void Trasy_krytyczne_dzialaja_mimo_nieukonczonej_konfiguracji(string sciezka, string powod)
    {
        Assert.True(
            KonfiguracjaStartowa.SciezkaDostepnaBezKonfiguracji(new PathString(sciezka)),
            $"Trasa {sciezka} musi byc dostepna bez konfiguracji: {powod}");
    }

    [Theory]
    [InlineData("/api/org/employees")]
    [InlineData("/api/time/timesheet/x")]
    [InlineData("/api/leave/requests/x")]
    [InlineData("/api/tasks")]
    public void Zwykle_trasy_aplikacji_sa_objete_blokada(string sciezka)
    {
        Assert.False(KonfiguracjaStartowa.SciezkaDostepnaBezKonfiguracji(new PathString(sciezka)));
    }

    /// <summary>
    /// Bramka kompletnosci: wypisuje, ile tras aplikacji obejmuje blokada. Gdyby ktos
    /// rozszerzyl bialą listę o zbyt ogolny prefiks (np. "/api"), blokada przestalaby
    /// cokolwiek chronic, a zaden z testow wyzej by tego nie zauwazyl.
    /// </summary>
    [Fact]
    public void Biala_lista_nie_przepuszcza_wiekszosci_aplikacji()
    {
        var zrodlo = _factory.Services.GetRequiredService<EndpointDataSource>();
        var trasy = zrodlo.Endpoints
            .OfType<RouteEndpoint>()
            .Select(endpoint => "/" + (endpoint.RoutePattern.RawText ?? string.Empty).TrimStart('/'))
            .Distinct()
            .ToList();

        Assert.NotEmpty(trasy);

        var przepuszczane = trasy.Count(t => KonfiguracjaStartowa.SciezkaDostepnaBezKonfiguracji(new PathString(t)));

        // Kreator, auth, Hub, health i techniczne to garstka — reszta ma byc chroniona.
        Assert.True(przepuszczane < trasy.Count / 2,
            $"Biala lista przepuszcza {przepuszczane} z {trasy.Count} tras — za szeroka.");
    }

    [Fact]
    public async Task Firma_bez_znacznika_nie_jest_blokowana()
    {
        // Tak wyglada kazda firma zalozona przed powstaniem kreatora.
        var firma = Guid.NewGuid();
        using var client = KlientFirmy(firma);

        var odpowiedz = await client.GetAsync("/api/org/employees");

        Assert.NotEqual(HttpStatusCode.Conflict, odpowiedz.StatusCode);
    }

    [Fact]
    public async Task Firma_z_nieukonczona_konfiguracja_dostaje_409_z_kodem()
    {
        var firma = Guid.NewGuid();
        await OznaczJakoWymagana(firma);
        using var client = KlientFirmy(firma);

        var odpowiedz = await client.GetAsync("/api/org/employees");

        Assert.Equal(HttpStatusCode.Conflict, odpowiedz.StatusCode);

        var tresc = await odpowiedz.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("SETUP_REQUIRED", tresc.GetProperty("errorCode").GetString());
    }

    [Fact]
    public async Task Kreator_dziala_mimo_blokady_i_potrafi_ja_zdjac()
    {
        var firma = Guid.NewGuid();
        await OznaczJakoWymagana(firma);
        using var client = KlientFirmy(firma);

        // Stan jest czytelny mimo blokady — inaczej interfejs nie wiedzialby, co pokazac.
        var stan = await client.GetFromJsonAsync<JsonElement>("/api/setup/state");
        Assert.True(stan.GetProperty("wymagana").GetBoolean());
        Assert.False(stan.GetProperty("ukonczona").GetBoolean());

        var zakonczenie = await client.PostAsync("/api/setup/complete", null);
        Assert.Equal(HttpStatusCode.NoContent, zakonczenie.StatusCode);

        // Po ukonczeniu reszta aplikacji odpowiada normalnie — bez czekania na wygasniecie
        // podrecznej pamieci, bo ukonczenie ja czysci.
        var poZakonczeniu = await client.GetAsync("/api/org/employees");
        Assert.NotEqual(HttpStatusCode.Conflict, poZakonczeniu.StatusCode);
    }

    private HttpClient KlientFirmy(Guid firma) => _factory.CreateAuthenticatedClient(
        userId: Guid.NewGuid(),
        tenantId: firma,
        permissions: ["org.view", "identity.view"],
        employeeId: Guid.NewGuid());

    private async Task OznaczJakoWymagana(Guid firma)
    {
        using var scope = _factory.Services.CreateScope();
        var konfiguracja = scope.ServiceProvider.GetRequiredService<IKonfiguracjaStartowaService>();
        await konfiguracja.OznaczJakoWymaganaAsync(firma);
    }
}

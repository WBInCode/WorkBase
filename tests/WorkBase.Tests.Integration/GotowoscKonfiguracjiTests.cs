using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;

namespace WorkBase.Tests.Integration;

/// <summary>
/// „Co jeszcze nie zadziała” — lista braków konfiguracji.
/// </summary>
/// <remarks>
/// Pusta firma jest tu przypadkiem najważniejszym, bo tak wygląda każda firma tuż po kreatorze
/// pierwszego startu: kreator zadaje trzy pytania i celowo nie pyta o resztę. Ten ekran jest
/// jedynym miejscem, z którego właściciel dowie się o brakach inaczej niż odkrywając je
/// w trakcie pracy.
/// </remarks>
[Collection("Integration")]
public class GotowoscKonfiguracjiTests
{
    private readonly WorkBaseWebFactory _factory;

    public GotowoscKonfiguracjiTests(WorkBaseWebFactory factory) => _factory = factory;

    [Fact]
    public async Task Pusta_firma_dostaje_liste_brakow_zamiast_pustej_odpowiedzi()
    {
        using var client = KlientAdmina(Guid.NewGuid());

        var gotowosc = await client.GetFromJsonAsync<JsonElement>("/api/konfiguracja/gotowosc");

        Assert.True(gotowosc.GetProperty("blokujace").GetInt32() > 0);

        var pozycje = gotowosc.GetProperty("pozycje").EnumerateArray().ToList();
        Assert.NotEmpty(pozycje);

        // Braki, ktore w pustej firmie MUSZA sie pojawic.
        var kody = pozycje.Select(p => p.GetProperty("kod").GetString()).ToList();
        Assert.Contains("pracownicy", kody);
        Assert.Contains("stanowiska", kody);
        Assert.Contains("grafik", kody);
    }

    /// <summary>
    /// Kazda pozycja ma powiedziec, CO NIE ZADZIALA — „brak stanowisk kierowniczych" nic nie
    /// znaczy dla nietechnicznego wlasciciela. Bez tresci ekran traci caly sens.
    /// </summary>
    [Fact]
    public async Task Kazda_pozycja_tlumaczy_skutek_i_prowadzi_do_ekranu()
    {
        using var client = KlientAdmina(Guid.NewGuid());

        var gotowosc = await client.GetFromJsonAsync<JsonElement>("/api/konfiguracja/gotowosc");

        foreach (var pozycja in gotowosc.GetProperty("pozycje").EnumerateArray())
        {
            var kod = pozycja.GetProperty("kod").GetString();
            Assert.False(string.IsNullOrWhiteSpace(pozycja.GetProperty("tytul").GetString()), $"pusty tytul w {kod}");
            Assert.False(string.IsNullOrWhiteSpace(pozycja.GetProperty("coNieZadziala").GetString()), $"brak skutku w {kod}");

            var sciezka = pozycja.GetProperty("sciezka").GetString();
            Assert.StartsWith("/", sciezka, StringComparison.Ordinal);

            var waga = pozycja.GetProperty("waga").GetString();
            Assert.Contains(waga, new[] { "blokuje", "warto" });
        }
    }

    [Fact]
    public async Task Bez_uprawnien_administracyjnych_lista_jest_niedostepna()
    {
        using var client = _factory.CreateAuthenticatedClient(
            userId: Guid.NewGuid(),
            tenantId: Guid.NewGuid(),
            permissions: ["org.view"],
            employeeId: Guid.NewGuid());

        var odpowiedz = await client.GetAsync("/api/konfiguracja/gotowosc");

        Assert.Equal(HttpStatusCode.Forbidden, odpowiedz.StatusCode);
    }

    private HttpClient KlientAdmina(Guid firma) => _factory.CreateAuthenticatedClient(
        userId: Guid.NewGuid(),
        tenantId: firma,
        permissions: ["org.view", "org.edit"],
        employeeId: Guid.NewGuid());
}

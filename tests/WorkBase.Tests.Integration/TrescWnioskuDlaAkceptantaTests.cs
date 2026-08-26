using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using WorkBase.Infrastructure.Persistence;
using WorkBase.Modules.Workflow.Domain.Entities;
using Xunit;

namespace WorkBase.Tests.Integration;

/// <summary>
/// Kto moze zobaczyc tresc wniosku ogolnego.
/// </summary>
/// <remarks>
/// <para>
/// Kolejka akceptacji pokazywala wylacznie pasek „zatwierdz / odrzuc" — akceptant decydowal,
/// nie widzac ani jednego pola z wypelnionego formularza.
/// </para>
/// <para>
/// Dostepu NIE opieramy na zakresie danych, tylko na tym, czy pytajacy jest akceptantem TEGO
/// obiegu. Przy zastepstwie zastepca bywa poza zakresem danych osoby zastepowanej, a mimo to
/// ma sprawe rozstrzygnac — zakres odcialby go od tresci, o ktorej ma zdecydowac.
/// </para>
/// </remarks>
[Collection("Integration")]
public class TrescWnioskuDlaAkceptantaTests
{
    private readonly WorkBaseWebFactory _factory;

    public TrescWnioskuDlaAkceptantaTests(WorkBaseWebFactory factory) => _factory = factory;

    [Fact]
    public async Task Wnioskodawca_widzi_tresc_wlasnego_wniosku()
    {
        var (firma, wniosekId, wnioskodawca, _) = await Zasiej();
        using var client = Klient(firma, wnioskodawca);

        var tresc = await client.GetFromJsonAsync<JsonElement>($"/api/wnioski/{wniosekId}");

        Assert.Equal("Wyjście prywatne", tresc.GetProperty("typNazwa").GetString());
        var pozycje = tresc.GetProperty("pozycje").EnumerateArray().ToList();
        Assert.Equal(2, pozycje.Count);
        Assert.Equal("Powód", pozycje[0].GetProperty("etykieta").GetString());
        Assert.Equal("wizyta u lekarza", pozycje[0].GetProperty("wartosc").GetString());
    }

    [Fact]
    public async Task Akceptant_tego_obiegu_widzi_tresc_mimo_ze_to_cudzy_wniosek()
    {
        var (firma, wniosekId, _, akceptant) = await Zasiej();
        using var client = Klient(firma, akceptant);

        var odpowiedz = await client.GetAsync($"/api/wnioski/{wniosekId}");

        Assert.Equal(HttpStatusCode.OK, odpowiedz.StatusCode);
    }

    /// <summary>
    /// Pole zostawione puste ma dojsc do akceptanta jako puste, a nie zniknac z listy —
    /// brak odpowiedzi bywa rownie istotny co odpowiedz.
    /// </summary>
    [Fact]
    public async Task Pole_niewypelnione_jest_widoczne_jako_puste()
    {
        var (firma, wniosekId, wnioskodawca, _) = await Zasiej();
        using var client = Klient(firma, wnioskodawca);

        var tresc = await client.GetFromJsonAsync<JsonElement>($"/api/wnioski/{wniosekId}");
        var pozycje = tresc.GetProperty("pozycje").EnumerateArray().ToList();

        var puste = pozycje.Single(p => p.GetProperty("etykieta").GetString() == "Uwagi");
        Assert.Equal(JsonValueKind.Null, puste.GetProperty("wartosc").ValueKind);
    }

    [Fact]
    public async Task Osoba_postronna_nie_dowiaduje_sie_nawet_ze_wniosek_istnieje()
    {
        var (firma, wniosekId, _, _) = await Zasiej();
        using var client = Klient(firma, Guid.NewGuid());

        var odpowiedz = await client.GetAsync($"/api/wnioski/{wniosekId}");

        // 404, nie 403 — inaczej sama odmowa potwierdzalaby istnienie cudzego wniosku.
        Assert.Equal(HttpStatusCode.NotFound, odpowiedz.StatusCode);
    }

    private HttpClient Klient(Guid firma, Guid employeeId) => _factory.CreateAuthenticatedClient(
        userId: Guid.NewGuid(),
        tenantId: firma,
        permissions: ["wnioski.view"],
        employeeId: employeeId);

    private async Task<(Guid Firma, Guid WniosekId, Guid Wnioskodawca, Guid Akceptant)> Zasiej()
    {
        var firma = Guid.NewGuid();
        var wnioskodawca = Guid.NewGuid();
        var akceptant = Guid.NewGuid();
        var instancja = Guid.NewGuid();

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<WorkBaseDbContext>();

        var typ = TypWniosku.Utworz(
            firma, "WYJSCIE", "Wyjście prywatne",
            [
                new PoleWniosku("powod", "Powód", TypPola.Tekst, Wymagane: true),
                new PoleWniosku("uwagi", "Uwagi", TypPola.Wielolinijkowy),
            ]).Value;
        db.Add(typ);
        await db.SaveChangesAsync();

        // Tylko "powod" wypelniony — "uwagi" celowo zostaja puste.
        var wniosek = Wniosek.Zloz(
            firma, typ.Id, wnioskodawca,
            new Dictionary<string, string?> { ["powod"] = "wizyta u lekarza" },
            wymagaAkceptacji: true);
        wniosek.PowiazZObiegiem(instancja);
        db.Add(wniosek);

        db.Add(ApprovalRequest.Create(firma, Guid.NewGuid(), instancja, wnioskodawca, akceptant));
        await db.SaveChangesAsync();

        return (firma, wniosek.Id, wnioskodawca, akceptant);
    }
}

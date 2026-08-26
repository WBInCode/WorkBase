using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using WorkBase.Infrastructure.Persistence;
using WorkBase.Modules.TimeTracking.Domain.Entities;
using Xunit;

namespace WorkBase.Tests.Integration;

/// <summary>
/// Lista anomalii czasu pracy nie moze pokazywac cudzych.
/// </summary>
/// <remarks>
/// <para>
/// <c>GetAnomaliesHandler</c> filtrowal WYLACZNIE po najemcy, a endpoint wymaga <c>time.view</c>,
/// ktore ma kazdy pracownik. Czyli dowolna osoba mogla pobrac anomalie calej firmy: kto sie
/// spoznil, kto nie zarejestrowal wejscia — z identyfikatorem pracownika, ktory rozwiazuje sie
/// do nazwiska przez <c>/api/org/employees</c> (rowniez <c>org.view</c>, rowniez u kazdego).
/// </para>
/// <para>
/// To ta sama klasa bledu, ktora wczesniej wystapila w pulpicie. Ten test nie jest objety
/// <c>ZakresDanychPracownikaTests</c>, bo tamten sprawdza trasy z identyfikatorem W ADRESIE,
/// a tutaj identyfikatory sa w tresci odpowiedzi.
/// </para>
/// </remarks>
[Collection("Integration")]
public class ZakresAnomaliiTests
{
    private readonly WorkBaseWebFactory _factory;

    private async Task ZasiejAnomalie(Guid firma, params Guid[] pracownicy)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<WorkBaseDbContext>();
        var dzisiaj = DateOnly.FromDateTime(DateTime.UtcNow);

        foreach (var pracownik in pracownicy)
        {
            db.Add(TimeAnomaly.Create(
                firma, pracownik, dzisiaj, AnomalyType.MissingClockIn, "brak wejscia"));
        }
        await db.SaveChangesAsync();
    }

    public ZakresAnomaliiTests(WorkBaseWebFactory factory) => _factory = factory;

    [Fact]
    public async Task Szeregowy_pracownik_nie_dostaje_cudzych_anomalii()
    {
        var firma = Guid.NewGuid();
        var ja = Guid.NewGuid();

        using var client = _factory.CreateAuthenticatedClient(
            userId: Guid.NewGuid(),
            tenantId: firma,
            // Dokladnie taki zestaw ma kazdy zalogowany pracownik.
            permissions: ["time.view", "org.view"],
            employeeId: ja);

        // Bez zasiania danych ten test byl by pusty w srodku: pusta lista przechodzi kazda
        // petle, wiec przeszedlby rowniez BEZ poprawki. Sadzimy wiec dwie anomalie — moja
        // i cudza — zeby bylo co odfiltrowac.
        var kolega = Guid.NewGuid();
        await ZasiejAnomalie(firma, ja, kolega);

        var odpowiedz = await client.GetAsync("/api/time/anomalies");
        Assert.Equal(HttpStatusCode.OK, odpowiedz.StatusCode);

        var lista = (await odpowiedz.Content.ReadFromJsonAsync<JsonElement>())
            .EnumerateArray().ToList();

        var identyfikatory = lista.Select(a => a.GetProperty("employeeId").GetString()).ToList();

        Assert.Contains(ja.ToString(), identyfikatory);
        Assert.DoesNotContain(kolega.ToString(), identyfikatory);
    }

    [Fact]
    public async Task Odrzucenie_anomalii_wymaga_uprawnienia_zarzadzania_czasem()
    {
        using var client = _factory.CreateAuthenticatedClient(
            userId: Guid.NewGuid(),
            tenantId: Guid.NewGuid(),
            permissions: ["time.view", "org.view"],
            employeeId: Guid.NewGuid());

        var odpowiedz = await client.PutAsync($"/api/time/anomalies/{Guid.NewGuid()}/dismiss", null);

        Assert.Equal(HttpStatusCode.Forbidden, odpowiedz.StatusCode);
    }
}

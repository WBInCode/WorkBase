using System.Net;
using Xunit;

namespace WorkBase.Tests.Integration;

/// <summary>
/// Pracownik nie moze czytac danych innego pracownika przez podmiane identyfikatora w adresie.
/// </summary>
/// <remarks>
/// Audyt wykryl szesc endpointow, ktore braly employeeId wprost ze sciezki i oddawaly dane
/// bez sprawdzenia, czyje one sa. Wymagaly jedynie uprawnien time.view / leave.view, ktore ma
/// KAZDY pracownik, wiec wystarczylo podmienic identyfikator w adresie, zeby zobaczyc karte
/// czasu pracy, grafik albo wnioski urlopowe kolegi (razem z polem "powod").
/// Ten sam warunek stal juz przy /api/leave/requests/{employeeId} — reszta go nie miala.
/// </remarks>
[Collection("Integration")]
public class DostepDoDanychInnegoPracownikaTests
{
    private static readonly Guid TenantId = Guid.Parse("00000000-0000-0000-0000-000000000001");

    private readonly WorkBaseWebFactory _factory;

    public DostepDoDanychInnegoPracownikaTests(WorkBaseWebFactory factory) => _factory = factory;

    public static TheoryData<string> SciezkiZDanymiPracownika() =>
    [
        "/api/time/timesheet/{id}",
        "/api/time/status/{id}",
        "/api/time/break-availability/{id}",
        "/api/time/schedules/{id}",
        "/api/time/corrections/{id}",
        "/api/workspace/my-day/{id}",
    ];

    [Theory]
    [MemberData(nameof(SciezkiZDanymiPracownika))]
    public async Task Pracownik_nie_odczyta_danych_kolegi(string wzorzec)
    {
        var ja = Guid.NewGuid();
        var kolega = Guid.NewGuid();

        using var client = _factory.CreateAuthenticatedClient(
            userId: Guid.NewGuid(),
            tenantId: TenantId,
            // Uprawnienia poziomu pracownika: bez zadnego "-team".
            permissions: ["time.view", "leave.view", "tasks.view", "workflow.view"],
            employeeId: ja);

        var response = await client.GetAsync(wzorzec.Replace("{id}", kolega.ToString()));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Theory]
    [MemberData(nameof(SciezkiZDanymiPracownika))]
    public async Task Pracownik_odczyta_dane_wlasne(string wzorzec)
    {
        var ja = Guid.NewGuid();

        using var client = _factory.CreateAuthenticatedClient(
            userId: Guid.NewGuid(),
            tenantId: TenantId,
            permissions: ["time.view", "leave.view", "tasks.view", "workflow.view"],
            employeeId: ja);

        var response = await client.GetAsync(wzorzec.Replace("{id}", ja.ToString()));

        Assert.NotEqual(HttpStatusCode.Forbidden, response.StatusCode);
    }
}

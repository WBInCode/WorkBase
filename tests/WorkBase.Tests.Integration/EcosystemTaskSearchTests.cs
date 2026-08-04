using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using WorkBase.Infrastructure.Persistence;
using WorkBase.Modules.Organization.Domain.Entities;
using WorkBase.Modules.Tasks.Domain.Entities;
using Xunit;
using TaskStatusEntity = WorkBase.Modules.Tasks.Domain.Entities.TaskStatus;

namespace WorkBase.Tests.Integration;

/// <summary>
/// Wyszukiwarka zadań udostępniona innym aplikacjom ekosystemu. Endpoint jest
/// anonimowy, więc jedyną ochroną są: wyłączenie sekcji, sekret w nagłówku
/// i zawężenie wyników do jednej osoby. Każde z nich ma tu swój test.
/// </summary>
[Collection("Integration")]
public sealed class EcosystemTaskSearchTests
{
    private const string Sekret = "sekret-wyszukiwarki-zadan";
    private static readonly Guid Najemca = Guid.NewGuid();

    private static async Task<HttpResponseMessage> Zapytaj(
        TaskSearchTestFactory factory, string? sekret, string query)
    {
        var client = factory.CreateClient();
        var request = new HttpRequestMessage(HttpMethod.Get, $"/api/ecosystem/tasks?{query}");
        if (sekret is not null) request.Headers.Add("x-wb-task-secret", sekret);
        return await client.SendAsync(request);
    }

    [Fact]
    public async Task NieskonfigurowanaSekcjaUkrywaEndpoint()
    {
        using var factory = new TaskSearchTestFactory { Secret = null };

        var response = await Zapytaj(factory, Sekret, "email=jan@firma.pl");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task WylaczonaWyszukiwarkaUkrywaEndpoint()
    {
        using var factory = new TaskSearchTestFactory { Secret = Sekret, Enabled = false };

        var response = await Zapytaj(factory, Sekret, "email=jan@firma.pl");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task ZlySekretJestOdrzucany()
    {
        using var factory = new TaskSearchTestFactory { Secret = Sekret };

        var response = await Zapytaj(factory, "nie-ten-sekret", "email=jan@firma.pl");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task BrakNaglowkaJestOdrzucany()
    {
        using var factory = new TaskSearchTestFactory { Secret = Sekret };

        var response = await Zapytaj(factory, null, "email=jan@firma.pl");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task BrakAdresuToBladZadania()
    {
        using var factory = new TaskSearchTestFactory { Secret = Sekret };

        var response = await Zapytaj(factory, Sekret, "q=raport");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task NieznanyAdresToPustyWynik()
    {
        // Inaczej pytająca aplikacja mogłaby sprawdzać, kto tu pracuje.
        using var factory = new TaskSearchTestFactory { Secret = Sekret };

        var response = await Zapytaj(factory, Sekret, "email=nikt@firma.pl");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Empty(await OdczytajZadania(response));
    }

    [Fact]
    public async Task ZwracaWylacznieOtwarteZadaniaTejOsoby()
    {
        using var factory = new TaskSearchTestFactory { Secret = Sekret };
        await Zasiej(factory);

        var response = await Zapytaj(factory, Sekret, "email=anna@firma.pl");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var zadania = await OdczytajZadania(response);
        var tytuly = zadania.Select(z => z.GetProperty("title").GetString()).ToList();

        Assert.Contains("Raport kwartalny", tytuly);
        // Zadanie kolegi nie może wyciec do innej aplikacji.
        Assert.DoesNotContain("Zadanie Bartka", tytuly);
        // Zamknięte zadania tylko zaśmiecałyby podpowiedzi.
        Assert.DoesNotContain("Sprawa zamknieta", tytuly);
    }

    [Fact]
    public async Task FrazaZawezaWynikBezWzgleduNaWielkoscLiter()
    {
        using var factory = new TaskSearchTestFactory { Secret = Sekret };
        await Zasiej(factory);

        var response = await Zapytaj(factory, Sekret, "email=anna@firma.pl&q=KWARTALNY");

        var tytuly = (await OdczytajZadania(response))
            .Select(z => z.GetProperty("title").GetString())
            .ToList();

        Assert.Equal(["Raport kwartalny"], tytuly);
    }

    private static async Task<List<JsonElement>> OdczytajZadania(HttpResponseMessage response)
    {
        var tresc = await response.Content.ReadFromJsonAsync<JsonElement>();
        return tresc.GetProperty("tasks").EnumerateArray().ToList();
    }

    private static async Task Zasiej(TaskSearchTestFactory factory)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<WorkBaseDbContext>();

        var anna = Employee.Create(Najemca, "Anna", "Nowak", "anna@firma.pl", null, DateTime.UtcNow);
        var bartek = Employee.Create(Najemca, "Bartek", "Kowal", "bartek@firma.pl", null, DateTime.UtcNow);
        db.Set<Employee>().AddRange(anna, bartek);

        var otwarty = TaskStatusEntity.Create(Najemca, "open", "Otwarte");
        var zamkniety = TaskStatusEntity.Create(Najemca, "done", "Zamkniete", isFinal: true);
        db.Set<TaskStatusEntity>().AddRange(otwarty, zamkniety);

        var priorytet = TaskPriority.Create(Najemca, "normal", "Normalny");
        db.Set<TaskPriority>().Add(priorytet);

        db.Set<TaskItem>().AddRange(
            TaskItem.Create(Najemca, "Raport kwartalny", otwarty.Id, priorytet.Id, anna.Id),
            TaskItem.Create(Najemca, "Sprawa zamknieta", zamkniety.Id, priorytet.Id, anna.Id),
            TaskItem.Create(Najemca, "Zadanie Bartka", otwarty.Id, priorytet.Id, bartek.Id));

        await db.SaveChangesAsync();
    }
}

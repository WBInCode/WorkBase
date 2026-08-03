using System.Net;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;
using WorkBase.Infrastructure.Ecosystem;
using WorkBase.Infrastructure.Persistence;
using WorkBase.Modules.Organization.Domain.Entities;
using WorkBase.Modules.Tasks.Domain.Entities;
using WorkBase.Modules.TimeTracking.Domain.Entities;
using Xunit;
using TaskPriority = WorkBase.Modules.Tasks.Domain.Entities.TaskPriority;
using TaskStatus = WorkBase.Modules.Tasks.Domain.Entities.TaskStatus;

namespace WorkBase.Tests.Integration;

/// <summary>
/// Sprawdza, ze brak konta w Rytmie nie jest traktowany jak awaria.
/// </summary>
/// <remarks>
/// Audyt produkcji: 43% zadan w tle konczylo sie bledem. Rytm ma 3 konta, a WorkBase
/// 9 pracownikow — piecioro bez konta dostawalo 404 USER_NOT_FOUND, co rzucalo wyjatek
/// i uruchamialo ponawianie. Efekt: ponad 5000 nieudanych wywolan na dobe, 1493 zablokowane
/// zadania i 35 MB w tabelach kolejki. Prawdziwe awarie ginely w tym szumie.
/// </remarks>
public sealed class EcosystemSnapshotJobTests
{
    private static readonly Guid TenantId = Guid.Parse("10000000-0000-0000-0000-000000000001");

    [Fact]
    public async Task Brak_konta_w_Rytmie_nie_wywala_zadania()
    {
        var (job, employeeId) = await PrzygotujZadanie(HttpStatusCode.NotFound, "{\"error\":\"USER_NOT_FOUND\"}");

        var wyjatek = await Record.ExceptionAsync(() => job.ExecuteAsync(TenantId, employeeId));

        Assert.Null(wyjatek);
    }

    [Fact]
    public async Task Prawdziwa_awaria_Rytmu_nadal_przerywa_zadanie()
    {
        // Wyciszenie 404 nie moze wyciszyc wszystkiego — inaczej realna awaria
        // integracji przechodzilaby niezauwazona.
        var (job, employeeId) = await PrzygotujZadanie(HttpStatusCode.InternalServerError, "boom");

        await Assert.ThrowsAsync<HttpRequestException>(() => job.ExecuteAsync(TenantId, employeeId));
    }

    [Fact]
    public async Task Zadania_pracownika_ida_do_Rytmu_razem_z_grafikiem()
    {
        var (job, employeeId, handler) = await PrzygotujZadanieZDanymi();

        await job.ExecuteAsync(TenantId, employeeId);

        var pakiet = JsonDocument.Parse(handler.OstatniaTresc!).RootElement;
        var zadania = pakiet.GetProperty("tasks").EnumerateArray().ToList();

        Assert.Single(zadania);
        Assert.Equal("Przeglad umowy najmu", zadania[0].GetProperty("title").GetString());
        Assert.Equal("IN_PROGRESS", zadania[0].GetProperty("status").GetString());
        Assert.Equal("HIGH", zadania[0].GetProperty("priority").GetString());
        Assert.Equal(
            "https://workbase.example.test/tasks/" + zadania[0].GetProperty("sourceRef").GetString(),
            zadania[0].GetProperty("url").GetString());
    }

    [Fact]
    public async Task Grafik_pracy_nie_oznacza_zajetosci()
    {
        // Rytm pokazywalby kazdego pracujacego jako zajetego przez cala zmiane,
        // gdyby grafik szedl bez tego rozroznienia.
        var (job, employeeId, handler) = await PrzygotujZadanieZDanymi();

        await job.ExecuteAsync(TenantId, employeeId);

        var wydarzenia = JsonDocument.Parse(handler.OstatniaTresc!).RootElement
            .GetProperty("events").EnumerateArray().ToList();
        var grafik = wydarzenia.Single(e => e.GetProperty("sourceRef").GetString()!.StartsWith("schedule:"));

        Assert.False(grafik.GetProperty("busy").GetBoolean());
    }

    [Fact]
    public async Task Zamkniete_zadanie_sprzed_okna_nie_jest_wysylane()
    {
        // Rytm anuluje u siebie zadania spoza pakietu, wiec zamkniete dawno temu
        // maja tam juz status DONE i nie ma potrzeby wozic ich w kazdym snapshocie.
        var (job, employeeId, handler) = await PrzygotujZadanieZDanymi();

        await job.ExecuteAsync(TenantId, employeeId);

        var tytuly = JsonDocument.Parse(handler.OstatniaTresc!).RootElement
            .GetProperty("tasks").EnumerateArray()
            .Select(t => t.GetProperty("title").GetString())
            .ToList();

        Assert.DoesNotContain("Stara sprawa", tytuly);
    }

    private static async Task<(EcosystemSnapshotJob Job, Guid EmployeeId, StubHandler Handler)> PrzygotujZadanieZDanymi()
    {
        var options = new DbContextOptionsBuilder<WorkBaseDbContext>()
            .UseInMemoryDatabase($"ecosystem-tests-{Guid.NewGuid():N}")
            .Options;
        var db = new WorkBaseDbContext(options);

        var employee = Employee.Create(TenantId, "Jan", "Testowy", "jan.testowy@example.com", null, DateTime.UtcNow.Date);
        db.Add(employee);

        var wToku = TaskStatus.Create(TenantId, "IN_PROGRESS", "W toku");
        var zamkniete = TaskStatus.Create(TenantId, "CLOSED", "Zamkniete", isFinal: true);
        var wysoki = TaskPriority.Create(TenantId, "HIGH", "Wysoki", sortOrder: 3);
        db.AddRange(wToku, zamkniete, wysoki);

        db.Add(TaskItem.Create(TenantId, "Przeglad umowy najmu", wToku.Id, wysoki.Id, employee.Id));
        var stare = TaskItem.Create(TenantId, "Stara sprawa", zamkniete.Id, wysoki.Id, employee.Id);
        stare.Complete(DateTime.UtcNow.AddDays(-90));
        db.Add(stare);

        var dzis = DateOnly.FromDateTime(DateTime.UtcNow);
        db.Add(Schedule.Create(TenantId, employee.Id, dzis, new TimeOnly(8, 0), new TimeOnly(16, 0)));
        await db.SaveChangesAsync();

        var handler = new StubHandler(HttpStatusCode.OK, "{\"ok\":true}");
        var factory = Substitute.For<IHttpClientFactory>();
        factory.CreateClient("RytmEcosystem").Returns(_ => new HttpClient(handler)
        {
            BaseAddress = new Uri("https://rytm.example.test/"),
        });

        var job = new EcosystemSnapshotJob(
            db,
            factory,
            Options.Create(new EcosystemOptions
            {
                Enabled = true,
                TenantId = TenantId,
                BaseUrl = "https://rytm.example.test/",
                Secret = "test",
                HubOrgId = "org",
                AppUrl = "https://workbase.example.test",
            }),
            NullLogger<EcosystemSnapshotJob>.Instance);

        return (job, employee.Id, handler);
    }

    private static async Task<(EcosystemSnapshotJob Job, Guid EmployeeId)> PrzygotujZadanie(
        HttpStatusCode kod, string tresc)
    {
        var options = new DbContextOptionsBuilder<WorkBaseDbContext>()
            .UseInMemoryDatabase($"ecosystem-tests-{Guid.NewGuid():N}")
            .Options;
        var db = new WorkBaseDbContext(options);

        var employee = Employee.Create(TenantId, "Jan", "Testowy", "jan.testowy@example.com", null, DateTime.UtcNow.Date);
        db.Add(employee);
        await db.SaveChangesAsync();

        var handler = new StubHandler(kod, tresc);
        var factory = Substitute.For<IHttpClientFactory>();
        factory.CreateClient("RytmEcosystem").Returns(_ => new HttpClient(handler)
        {
            BaseAddress = new Uri("https://rytm.example.test/"),
        });

        var job = new EcosystemSnapshotJob(
            db,
            factory,
            Options.Create(new EcosystemOptions
            {
                Enabled = true,
                TenantId = TenantId,
                BaseUrl = "https://rytm.example.test/",
                Secret = "test",
                HubOrgId = "org",
            }),
            NullLogger<EcosystemSnapshotJob>.Instance);

        return (job, employee.Id);
    }

    private sealed class StubHandler(HttpStatusCode kod, string tresc) : HttpMessageHandler
    {
        public string? OstatniaTresc { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (request.Content is not null)
                OstatniaTresc = await request.Content.ReadAsStringAsync(cancellationToken);
            return new HttpResponseMessage(kod) { Content = new StringContent(tresc) };
        }
    }
}

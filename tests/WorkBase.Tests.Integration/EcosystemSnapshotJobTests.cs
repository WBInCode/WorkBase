using System.Net;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;
using WorkBase.Infrastructure.Ecosystem;
using WorkBase.Infrastructure.Persistence;
using WorkBase.Modules.Organization.Domain.Entities;
using Xunit;

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
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
            => Task.FromResult(new HttpResponseMessage(kod) { Content = new StringContent(tresc) });
    }
}

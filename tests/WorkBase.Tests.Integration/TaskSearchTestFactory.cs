using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using WorkBase.Infrastructure.Persistence;

namespace WorkBase.Tests.Integration;

/// <summary>
/// Samodzielna fabryka dla <see cref="EcosystemTaskSearchTests"/>. Nie nadbudowuje
/// <see cref="WorkBaseWebFactory"/> z tego samego powodu co <see cref="WebhookTestFactory"/>
/// — patrz komentarz w tamtej klasie.
/// </summary>
public sealed class TaskSearchTestFactory : WebApplicationFactory<WorkBase.Host.Program>
{
    /// <summary>
    /// Usunięcie samych <c>DbContextOptions</c> nie wystarcza: usługi dostawcy Npgsql
    /// zostają w kontenerze i EF przerywa każde zapytanie komunikatem o dwóch
    /// zarejestrowanych dostawcach. Dlatego kontekst testowy dostaje własny,
    /// wewnętrzny dostawca usług wyłącznie z bazą w pamięci.
    /// </summary>
    private static readonly IServiceProvider UslugiEfWPamieci = new ServiceCollection()
        .AddEntityFrameworkInMemoryDatabase()
        .BuildServiceProvider();

    /// <summary>Null = sekcja nieskonfigurowana, czyli wyszukiwarka wyłączona.</summary>
    public string? Secret { get; init; }

    public bool Enabled { get; init; } = true;

    public string DatabaseName { get; } = "WorkBase_TaskSearch_Test_" + Guid.NewGuid().ToString("N");

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        if (Secret is not null)
        {
            builder.ConfigureAppConfiguration((_, config) =>
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["TaskSearch:Enabled"] = Enabled ? "true" : "false",
                    ["TaskSearch:Secret"] = Secret,
                }));
        }

        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<WorkBaseDbContext>();
            services.RemoveAll<DbContextOptions<WorkBaseDbContext>>();
            services.RemoveAll<DbContextOptions>();

            services.AddDbContext<WorkBaseDbContext>((_, options) =>
                options
                    .UseInMemoryDatabase(DatabaseName)
                    .UseInternalServiceProvider(UslugiEfWPamieci));

            var hostedServiceDescriptors = services
                .Where(d => d.ServiceType == typeof(IHostedService))
                .ToList();
            foreach (var hd in hostedServiceDescriptors)
                services.Remove(hd);
        });
    }
}

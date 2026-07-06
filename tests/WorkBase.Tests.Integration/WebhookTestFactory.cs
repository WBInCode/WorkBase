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
/// Standalone factory used only by <see cref="WebhookSecurityTests"/>.
///
/// It intentionally does NOT derive its host via <c>WorkBaseWebFactory.WithWebHostBuilder</c>:
/// layering a second host on top of an already-built <see cref="WebApplicationFactory{TEntryPoint}"/>
/// causes EF Core to register both the Npgsql and InMemory providers in the same internal
/// service provider ("Only a single database provider can be registered..."), because the
/// original host's EF service registrations aren't fully torn down. Building a fresh,
/// independent factory per test avoids that conflict at the cost of a slightly slower test.
/// </summary>
public sealed class WebhookTestFactory : WebApplicationFactory<WorkBase.Host.Program>
{
    /// <summary>Set before the first call to CreateClient()/Server. Null = no Stripe secret configured.</summary>
    public string? WebhookSecret { get; init; }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        if (WebhookSecret is not null)
        {
            builder.ConfigureAppConfiguration((_, config) =>
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Stripe:WebhookSecret"] = WebhookSecret,
                }));
        }

        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<DbContextOptions<WorkBaseDbContext>>();
            services.RemoveAll<DbContextOptions>();

            services.AddDbContext<WorkBaseDbContext>((_, options) =>
                options.UseInMemoryDatabase("WorkBase_Webhook_Test_" + Guid.NewGuid().ToString("N")));

            // No hosted service (Hangfire's BackgroundJobServer) should run in tests.
            var hostedServiceDescriptors = services
                .Where(d => d.ServiceType == typeof(IHostedService))
                .ToList();
            foreach (var hd in hostedServiceDescriptors)
                services.Remove(hd);
        });
    }
}

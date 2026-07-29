using System.Net;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using WorkBase.Contracts;
using WorkBase.Infrastructure.Auth;
using WorkBase.Infrastructure.HubPlatform;
using WorkBase.Infrastructure.Persistence;
using WorkBase.Modules.Organization.Domain.Entities;
using Xunit;

namespace WorkBase.Tests.Integration;

public sealed class HubKioskProvisioningSyncTests
{
    [Fact]
    public async Task Active_instance_sync_provisions_kiosk_for_owner_before_first_login()
    {
        var tenantId = Guid.NewGuid();
        var databaseName = $"hub-kiosk-sync-{Guid.NewGuid():N}";
        var provisioning = Substitute.For<ITenantProvisioningService>();
        provisioning.EnsureHubTenantAsync(
                Arg.Any<HubTenantRegistration>(),
                Arg.Any<CancellationToken>())
            .Returns(new HubTenantProvisioningResult(tenantId, Created: true));
        var kiosk = Substitute.For<IKioskAccountProvisioningService>();
        kiosk.EnsureForTenantAsync(
                tenantId,
                "owner@acme.example",
                credentialsCanBeReturned: false,
                Arg.Any<CancellationToken>())
            .Returns(new KioskAccountProvisioningResult(
                "kiosk-acme",
                "https://workbase.example/kiosk?realm=",
                TemporaryPassword: null,
                CredentialsEmailSent: true,
                CredentialsDelivered: true));

        var services = new ServiceCollection();
        services.AddDbContext<WorkBaseDbContext>(options => options.UseInMemoryDatabase(databaseName));
        services.AddMemoryCache();
        services.AddScoped(_ => provisioning);
        services.AddScoped(_ => kiosk);
        services.AddSingleton<TenantAccessCache>();
        await using var provider = services.BuildServiceProvider();

        await using (var seedScope = provider.CreateAsyncScope())
        {
            var db = seedScope.ServiceProvider.GetRequiredService<WorkBaseDbContext>();
            var tenant = Tenant.Create("Acme", "acme");
            typeof(Tenant).GetProperty(nameof(Tenant.Id))!.SetValue(tenant, tenantId);
            tenant.LinkToHub("hub-org", "hub-instance");
            db.Add(tenant);
            await db.SaveChangesAsync();
        }

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Hub:Enabled"] = "true",
                ["Hub:BaseUrl"] = "https://hub.example",
                ["Hub:ClientId"] = "workbase",
                ["Hub:ClientSecret"] = "hub-secret",
            })
            .Build();
        var httpClientFactory = Substitute.For<IHttpClientFactory>();
        httpClientFactory.CreateClient("hub-platform").Returns(new HttpClient(new ConfigHandler()));
        var logger = new CapturingLogger();
        var service = new HubEntitlementsSyncService(
            httpClientFactory,
            configuration,
            provider,
            logger);

        var config = await service.GetInstanceConfigAsync("hub-instance");
        Assert.True(config is not null, string.Join(Environment.NewLine, logger.Messages));

        var result = await service.SyncInstanceAsync("hub-instance");

        Assert.True(
            result is not null,
            logger.Exception?.ToString() ?? string.Join(Environment.NewLine, logger.Messages));
        Assert.True(result.AccessEnabled);
        await kiosk.Received(1).EnsureForTenantAsync(
            tenantId,
            "owner@acme.example",
            credentialsCanBeReturned: false,
            Arg.Any<CancellationToken>());
    }

    private sealed class ConfigHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            const string body = """
                {
                  "instanceId": "hub-instance",
                  "orgId": "hub-org",
                  "orgSlug": "acme",
                  "orgName": "Acme",
                  "productKey": "workbase",
                  "status": "active",
                  "plan": "standard",
                  "modules": [],
                  "customDomain": null,
                  "administrator": {
                    "email": "owner@acme.example",
                    "displayName": "Acme Owner"
                  }
                }
                """;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(body, Encoding.UTF8, "application/json"),
            });
        }
    }

    private sealed class CapturingLogger : Microsoft.Extensions.Logging.ILogger<HubEntitlementsSyncService>
    {
        public Exception? Exception { get; private set; }
        public List<string> Messages { get; } = [];

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(Microsoft.Extensions.Logging.LogLevel logLevel) => true;

        public void Log<TState>(
            Microsoft.Extensions.Logging.LogLevel logLevel,
            Microsoft.Extensions.Logging.EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            Messages.Add(formatter(state, exception));
            if (exception is not null)
                Exception = exception;
        }
    }
}

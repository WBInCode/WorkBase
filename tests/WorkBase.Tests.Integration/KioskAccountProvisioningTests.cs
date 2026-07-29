using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using System.Net;
using System.Text;
using WorkBase.Contracts;
using WorkBase.Infrastructure.Persistence;
using WorkBase.Infrastructure.Services;
using WorkBase.Modules.Organization.Domain.Entities;
using Xunit;

namespace WorkBase.Tests.Integration;

public sealed class KioskAccountProvisioningTests
{
    [Fact]
    public async Task Hub_tenant_credentials_use_authenticated_idempotent_delivery_endpoint()
    {
        await using var db = CreateDbContext();
        var tenant = Tenant.Create("Acme", "acme");
        tenant.LinkToHub("hub-org", "hub-instance");
        db.Add(tenant);
        await db.SaveChangesAsync();

        var keycloak = Substitute.For<IKeycloakAdminService>();
        keycloak.PrepareKioskUserAsync(
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<Dictionary<string, string>>(),
                Arg.Any<CancellationToken>())
            .Returns(new KeycloakKioskAccountResult("kiosk-user-id", CredentialsIssued: true));
        keycloak.MarkKioskCredentialsDeliveredAsync(
                Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(true);
        var email = Substitute.For<IEmailSender>();
        var handler = new RecordingHandler();
        var httpClientFactory = Substitute.For<IHttpClientFactory>();
        httpClientFactory.CreateClient("hub-platform").Returns(new HttpClient(handler));
        var service = CreateService(db, keycloak, email, httpClientFactory, hubEnabled: true);

        var result = await service.EnsureForTenantAsync(
            tenant.Id,
            "admin@acme.example",
            credentialsCanBeReturned: false);

        Assert.NotNull(result);
        Assert.True(result.CredentialsEmailSent);
        Assert.True(result.CredentialsDelivered);
        Assert.Equal(
            "https://hub.example/api/v1/instances/hub-instance/kiosk-credentials",
            handler.RequestUri?.ToString());
        Assert.Equal("workbase", handler.ClientId);
        Assert.Equal("hub-secret", handler.ClientSecret);
        Assert.Matches("^[a-f0-9]{64}$", handler.IdempotencyKey);
        Assert.Contains("admin@acme.example", handler.Body);
        Assert.Contains("kiosk-acme", handler.Body);
        await email.DidNotReceiveWithAnyArgs().SendAsync(default!, default!, default!, default);
        await keycloak.Received(1).MarkKioskCredentialsDeliveredAsync(
            "workbase",
            "kiosk-user-id",
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task New_kiosk_credentials_are_emailed_and_marked_as_delivered()
    {
        await using var db = CreateDbContext();
        var tenant = Tenant.Create("Acme", "acme");
        db.Add(tenant);
        await db.SaveChangesAsync();

        var keycloak = Substitute.For<IKeycloakAdminService>();
        keycloak.PrepareKioskUserAsync(
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<Dictionary<string, string>>(),
                Arg.Any<CancellationToken>())
            .Returns(new KeycloakKioskAccountResult("kiosk-user-id", CredentialsIssued: true));
        keycloak.MarkKioskCredentialsDeliveredAsync(
                Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(true);
        var email = Substitute.For<IEmailSender>();
        var service = CreateService(db, keycloak, email);

        var result = await service.EnsureForTenantAsync(
            tenant.Id,
            "admin@acme.example",
            credentialsCanBeReturned: false);

        Assert.NotNull(result);
        Assert.Equal("kiosk-acme", result.Username);
        Assert.Equal("https://workbase.example/kiosk?realm=", result.LoginUrl);
        Assert.Null(result.TemporaryPassword);
        Assert.True(result.CredentialsEmailSent);
        Assert.True(result.CredentialsDelivered);
        await email.Received(1).SendAsync(
            "admin@acme.example",
            Arg.Is<string>(subject => subject.Contains("WorkBase Kiosk")),
            Arg.Is<string>(body => body.Contains("kiosk-acme") && body.Contains("https://workbase.example/kiosk")),
            Arg.Any<CancellationToken>());
        await keycloak.Received(1).MarkKioskCredentialsDeliveredAsync(
            "workbase",
            "kiosk-user-id",
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Existing_delivered_kiosk_does_not_send_or_rotate_credentials()
    {
        await using var db = CreateDbContext();
        var tenant = Tenant.Create("Acme", "acme");
        db.Add(tenant);
        await db.SaveChangesAsync();

        var keycloak = Substitute.For<IKeycloakAdminService>();
        keycloak.PrepareKioskUserAsync(
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<Dictionary<string, string>>(),
                Arg.Any<CancellationToken>())
            .Returns(new KeycloakKioskAccountResult("kiosk-user-id", CredentialsIssued: false));
        var email = Substitute.For<IEmailSender>();
        var service = CreateService(db, keycloak, email);

        var result = await service.EnsureForTenantAsync(
            tenant.Id,
            "admin@acme.example",
            credentialsCanBeReturned: false);

        Assert.NotNull(result);
        Assert.True(result.CredentialsDelivered);
        Assert.False(result.CredentialsEmailSent);
        Assert.Null(result.TemporaryPassword);
        await email.DidNotReceiveWithAnyArgs().SendAsync(default!, default!, default!, default);
        await keycloak.DidNotReceiveWithAnyArgs()
            .MarkKioskCredentialsDeliveredAsync(default!, default!, default);
    }

    [Fact]
    public async Task Failed_email_keeps_kiosk_credentials_pending_for_retry()
    {
        await using var db = CreateDbContext();
        var tenant = Tenant.Create("Acme", "acme");
        db.Add(tenant);
        await db.SaveChangesAsync();

        var keycloak = Substitute.For<IKeycloakAdminService>();
        keycloak.PrepareKioskUserAsync(
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<Dictionary<string, string>>(),
                Arg.Any<CancellationToken>())
            .Returns(new KeycloakKioskAccountResult("kiosk-user-id", CredentialsIssued: true));
        var email = Substitute.For<IEmailSender>();
        email.SendAsync(
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<CancellationToken>())
            .Returns(Task.FromException(new InvalidOperationException("SMTP unavailable")));
        var service = CreateService(db, keycloak, email);

        var result = await service.EnsureForTenantAsync(
            tenant.Id,
            "admin@acme.example",
            credentialsCanBeReturned: false);

        Assert.NotNull(result);
        Assert.False(result.CredentialsEmailSent);
        Assert.False(result.CredentialsDelivered);
        await keycloak.DidNotReceiveWithAnyArgs()
            .MarkKioskCredentialsDeliveredAsync(default!, default!, default);
    }

    private static KioskAccountProvisioningService CreateService(
        WorkBaseDbContext db,
        IKeycloakAdminService keycloak,
        IEmailSender email,
        IHttpClientFactory? httpClientFactory = null,
        bool hubEnabled = false)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Keycloak:Realm"] = "workbase",
                ["Hub:FrontendUrl"] = "https://workbase.example",
                ["Hub:Enabled"] = hubEnabled.ToString(),
                ["Hub:BaseUrl"] = "https://hub.example",
                ["Hub:ClientId"] = "workbase",
                ["Hub:ClientSecret"] = "hub-secret",
            })
            .Build();
        return new KioskAccountProvisioningService(
            db,
            keycloak,
            email,
            httpClientFactory ?? Substitute.For<IHttpClientFactory>(),
            configuration,
            NullLogger<KioskAccountProvisioningService>.Instance);
    }

    private static WorkBaseDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<WorkBaseDbContext>()
            .UseInMemoryDatabase($"kiosk-account-tests-{Guid.NewGuid():N}")
            .Options;
        return new WorkBaseDbContext(options);
    }

    private sealed class RecordingHandler : HttpMessageHandler
    {
        public Uri? RequestUri { get; private set; }
        public string? ClientId { get; private set; }
        public string? ClientSecret { get; private set; }
        public string? IdempotencyKey { get; private set; }
        public string Body { get; private set; } = string.Empty;

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            RequestUri = request.RequestUri;
            ClientId = request.Headers.GetValues("x-sso-client-id").Single();
            ClientSecret = request.Headers.GetValues("x-sso-secret").Single();
            IdempotencyKey = request.Headers.GetValues("idempotency-key").Single();
            Body = request.Content is null
                ? string.Empty
                : await request.Content.ReadAsStringAsync(cancellationToken);
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{\"delivered\":true}", Encoding.UTF8, "application/json"),
            };
        }
    }
}

using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using WorkBase.Infrastructure.Persistence;
using WorkBase.Shared.Auth;

namespace WorkBase.Tests.Integration;

public class WorkBaseWebFactory : WebApplicationFactory<WorkBase.Host.Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureTestServices(services =>
        {
            // Replace EF Core DbContext with InMemory
            services.RemoveAll<DbContextOptions<WorkBaseDbContext>>();
            services.RemoveAll<DbContextOptions>();
            var descriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<WorkBaseDbContext>));
            if (descriptor is not null) services.Remove(descriptor);

            services.AddDbContext<WorkBaseDbContext>((sp, options) =>
            {
                options.UseInMemoryDatabase("WorkBase_Test_" + Guid.NewGuid().ToString("N"));
            });

            // Remove all IHostedService registrations (Hangfire's BackgroundJobServer is the
            // only one registered app-wide). Hangfire registers it via a factory delegate
            // (AddHangfireServer), so ImplementationType is null and a Hangfire-name filter
            // silently fails to match it — the real server then starts against Postgres in
            // every test host and hangs for ~60s on shutdown when the host is disposed
            // (e.g. via WithWebHostBuilder in per-test hosts). Removing all IHostedService
            // descriptors is safe here since no other hosted service is registered.
            var hostedServiceDescriptors = services
                .Where(d => d.ServiceType == typeof(IHostedService))
                .ToList();
            foreach (var hd in hostedServiceDescriptors)
                services.Remove(hd);

            // Replace auth with test scheme
            services.AddAuthentication("Test")
                .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>("Test", _ => { });

            services.PostConfigure<AuthenticationOptions>(options =>
            {
                options.DefaultAuthenticateScheme = "Test";
                options.DefaultChallengeScheme = "Test";
            });

            // Replace IPermissionService with test implementation
            services.RemoveAll<IPermissionService>();
            services.AddScoped<IPermissionService, TestPermissionService>();
        });
    }

    public HttpClient CreateAuthenticatedClient(
        Guid? userId = null,
        Guid? tenantId = null,
        string[]? permissions = null)
    {
        var client = CreateClient();

        if (userId.HasValue)
            client.DefaultRequestHeaders.Add("X-Test-Sub", userId.Value.ToString());
        if (tenantId.HasValue)
            client.DefaultRequestHeaders.Add("X-Test-Tenant", tenantId.Value.ToString());
        if (permissions is { Length: > 0 })
        {
            foreach (var perm in permissions)
                client.DefaultRequestHeaders.Add("X-Test-Permission", perm);
        }

        return client;
    }

    public HttpClient CreateUnauthenticatedClient()
    {
        var client = CreateClient();
        client.DefaultRequestHeaders.Add("X-Test-Anonymous", "true");
        return client;
    }
}

internal sealed class TestAuthHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (Request.Headers.ContainsKey("X-Test-Anonymous"))
            return Task.FromResult(AuthenticateResult.NoResult());

        var claims = new List<Claim>();

        if (Request.Headers.TryGetValue("X-Test-Sub", out var sub))
            claims.Add(new Claim("sub", sub.ToString()));
        else
            claims.Add(new Claim("sub", Guid.NewGuid().ToString()));

        if (Request.Headers.TryGetValue("X-Test-Tenant", out var tenant))
            claims.Add(new Claim("tenant_id", tenant.ToString()));

        if (Request.Headers.TryGetValue("X-Test-Permission", out var perms))
        {
            foreach (var perm in perms)
            {
                if (!string.IsNullOrEmpty(perm))
                    claims.Add(new Claim("permission", perm));
            }
        }

        var identity = new ClaimsIdentity(claims, "Test");
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, "Test");

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}

/// <summary>
/// Test implementation of IPermissionService that reads permissions from claims
/// set by TestAuthHandler via X-Test-Permission headers.
/// </summary>
internal sealed class TestPermissionService(IHttpContextAccessor httpContextAccessor) : IPermissionService
{
    public Task<IReadOnlySet<string>> GetUserPermissionsAsync(
        Guid userId, Guid tenantId, CancellationToken cancellationToken = default)
    {
        var user = httpContextAccessor.HttpContext?.User;
        var permissions = user?.FindAll("permission")
            .Select(c => c.Value)
            .ToHashSet()
            ?? new HashSet<string>();

        return Task.FromResult<IReadOnlySet<string>>(permissions);
    }

    public Task<bool> HasPermissionAsync(
        Guid userId, Guid tenantId, string permission, CancellationToken cancellationToken = default)
    {
        var user = httpContextAccessor.HttpContext?.User;
        var hasPermission = user?.FindAll("permission").Any(c => c.Value == permission) ?? false;
        return Task.FromResult(hasPermission);
    }
}

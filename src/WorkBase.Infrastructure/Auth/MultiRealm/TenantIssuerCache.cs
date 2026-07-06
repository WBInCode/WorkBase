using System.Collections.Concurrent;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using WorkBase.Infrastructure.Persistence;
using WorkBase.Modules.Organization.Domain.Entities;

namespace WorkBase.Infrastructure.Auth.MultiRealm;

/// <summary>
/// In-memory cache of "issuer URL -> TenantId" for every tenant that has its own dedicated
/// Keycloak realm (<see cref="Tenant.KeycloakRealmName"/>), plus the original shared realm.
///
/// EXISTS BECAUSE: <see cref="Microsoft.IdentityModel.Tokens.TokenValidationParameters"/>'s
/// IssuerValidator/IssuerSigningKeyResolver delegates are synchronous and run on every single
/// request's authentication — they must never block on a database call. This cache is
/// refreshed periodically in the background (<see cref="TenantIssuerCacheRefreshService"/>)
/// so the hot-path check is just an in-memory dictionary lookup.
///
/// SECURITY NOTE: only issuers present in this cache are ever accepted or have their JWKS
/// fetched (see DynamicIssuerSigningKeyResolver) — an attacker-supplied arbitrary "iss" claim
/// can never cause an outbound HTTP call or be treated as valid. New tenant realms only become
/// acceptable after the next refresh cycle (see RefreshIntervalSeconds), which is an
/// intentional trade-off: slightly delayed availability of newly onboarded realms in exchange
/// for never trusting an issuer we haven't independently confirmed exists in our own Tenant table.
/// </summary>
public sealed class TenantIssuerCache(IConfiguration configuration)
{
    private readonly ConcurrentDictionary<string, Guid> _issuerToTenantId = new(StringComparer.Ordinal);

    /// <summary>Base URL of the shared Keycloak server (e.g. "https://workbase-auth.onrender.com"), common to all realms.</summary>
    public string KeycloakBaseUrl { get; } =
        (configuration["Keycloak:AdminUrl"] ?? configuration["Keycloak:Authority"]!.Replace("/realms/workbase", ""))
        .TrimEnd('/');

    public bool IsValidIssuer(string issuer) => _issuerToTenantId.ContainsKey(issuer);

    public bool TryGetTenantId(string issuer, out Guid tenantId) => _issuerToTenantId.TryGetValue(issuer, out tenantId);

    public string BuildIssuer(string realmName) => $"{KeycloakBaseUrl}/realms/{realmName}";

    internal void ReplaceAll(IReadOnlyDictionary<string, Guid> issuerToTenantId)
    {
        _issuerToTenantId.Clear();
        foreach (var (issuer, tenantId) in issuerToTenantId)
        {
            _issuerToTenantId[issuer] = tenantId;
        }
    }
}

/// <summary>
/// Periodically refreshes <see cref="TenantIssuerCache"/> from the Tenant table. Only active
/// when Keycloak:MultiRealmEnabled is true (see AuthenticationExtensions) — inert by default,
/// so existing single-realm deployments are completely unaffected.
/// </summary>
public sealed class TenantIssuerCacheRefreshService(
    TenantIssuerCache cache,
    IServiceScopeFactory scopeFactory,
    IConfiguration configuration,
    ILogger<TenantIssuerCacheRefreshService> logger) : BackgroundService
{
    private static readonly TimeSpan RefreshInterval = TimeSpan.FromMinutes(5);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!configuration.GetValue<bool>("Keycloak:MultiRealmEnabled"))
        {
            logger.LogInformation("Multi-realm Keycloak disabled (Keycloak:MultiRealmEnabled=false) — issuer cache refresh not started.");
            return;
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await RefreshAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                // Never let a transient DB/refresh failure crash the whole process — worst
                // case, the cache keeps serving its last-known-good state until it recovers.
                logger.LogError(ex, "Failed to refresh tenant issuer cache");
            }

            try
            {
                await Task.Delay(RefreshInterval, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                // shutting down
            }
        }
    }

    private async Task RefreshAsync(CancellationToken ct)
    {
        using var scope = scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<WorkBaseDbContext>();

        var tenantsWithRealm = await dbContext.Set<Tenant>()
            .AsNoTracking()
            .Where(t => t.KeycloakRealmName != null)
            .Select(t => new { t.Id, t.KeycloakRealmName })
            .ToListAsync(ct);

        var map = new Dictionary<string, Guid>(StringComparer.Ordinal);

        // Always accept the original shared realm (tenants not yet migrated to a dedicated realm).
        var sharedRealm = configuration["Keycloak:Realm"] ?? "workbase";
        map[cache.BuildIssuer(sharedRealm)] = Guid.Empty; // shared realm: TenantId resolved from tenant_id claim, not from issuer

        foreach (var t in tenantsWithRealm)
        {
            map[cache.BuildIssuer(t.KeycloakRealmName!)] = t.Id;
        }

        cache.ReplaceAll(map);
        logger.LogInformation("Tenant issuer cache refreshed: {Count} known issuers", map.Count);
    }
}

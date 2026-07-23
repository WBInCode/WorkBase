using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using WorkBase.Contracts;
using WorkBase.Infrastructure.Auth;
using WorkBase.Infrastructure.Persistence;
using WorkBase.Modules.Identity.Domain.Entities;
using WorkBase.Modules.Organization.Domain.Entities;
using WorkBase.Shared.Modules;

namespace WorkBase.Infrastructure.HubPlatform;

/// <summary>
/// Konfiguracja integracji z Hubem ekosystemu (wb-platform).
/// Integracja jest OPCJONALNA — bez <c>Hub:Enabled=true</c> WorkBase działa jak dotychczas
/// (lokalne feature flags), wzorzec identyczny jak w dziennik-v2 (env HUB_*).
/// </summary>
public sealed class HubOptions
{
    public const string SectionName = "Hub";

    public bool Enabled { get; init; }
    /// <summary>Baza API Huba, np. http://localhost:4100.</summary>
    public string BaseUrl { get; init; } = "";
    /// <summary>Id instancji produktu "workbase" w Hubie (ProductInstance.id).</summary>
    public string InstanceId { get; init; } = "";
    /// <summary>Klient SSO produktu (x-sso-client-id / x-sso-secret) do Entitlements API.</summary>
    public string ClientId { get; init; } = "workbase";
    public string ClientSecret { get; init; } = "";
    /// <summary>Sekret HMAC webhooków Huba (x-wb-signature).</summary>
    public string WebhookSecret { get; init; } = "";
    /// <summary>Enables delivery of queued employee access changes after the HUB endpoints are deployed.</summary>
    public bool EmployeeAccessSyncEnabled { get; init; }
    /// <summary>Enables per-user InstanceAccess verification after the HUB access-check endpoint is deployed.</summary>
    public bool UserAccessCheckEnabled { get; init; }
    /// <summary>
    /// Legacy bootstrap mapping. If set together with <see cref="InstanceId"/>, the first sync
    /// links that existing tenant to HUB instead of creating a duplicate. New organizations do
    /// not use this setting.
    /// </summary>
    public Guid TenantId { get; init; }
    /// <summary>Issuer w tokenach handoff Huba (JWT_ISSUER, publiczny origin, np. https://wb-partners.pl).</summary>
    public string Issuer { get; init; } = "";
    /// <summary>Publiczny adres frontendu WorkBase, dokąd wraca przeglądarka po JIT-provisioningu.</summary>
    public string FrontendUrl { get; init; } = "";
}

public sealed record HubInstanceConfig(
    string InstanceId,
    string OrgId,
    string OrgSlug,
    string ProductKey,
    string Status,
    string Plan,
    string[] Modules,
    string? CustomDomain,
    string? OrgName = null,
    string? OrganizationName = null);

public sealed record HubTenantSyncResult(
    Guid TenantId,
    string OrganizationId,
    string ProductInstanceId,
    string Status,
    bool AccessEnabled,
    bool TenantCreated);

/// <summary>
/// Synchronizuje włączone moduły z Hub Entitlements API
/// (<c>GET /api/v1/instances/{id}/config</c>) do lokalnych FeatureFlags tenanta.
/// Wołane przy starcie aplikacji oraz po webhooku <c>entitlements.updated</c> —
/// dzięki temu operator Huba centralnie steruje modułami WorkBase
/// (TenantBehavior + nawigacja frontendu czytają te same flagi co dotychczas).
/// </summary>
public sealed class HubEntitlementsSyncService(
    IHttpClientFactory httpClientFactory,
    IConfiguration configuration,
    IServiceProvider serviceProvider,
    ILogger<HubEntitlementsSyncService> logger)
{
    public HubOptions Options =>
        configuration.GetSection(HubOptions.SectionName).Get<HubOptions>() ?? new HubOptions();

    /// <summary>Synchronizuje legacy/skonfigurowaną instancję. Nowy kod powinien podać instanceId.</summary>
    public async Task<bool> SyncAsync(CancellationToken ct = default)
    {
        var opts = Options;
        if (!opts.Enabled || string.IsNullOrWhiteSpace(opts.BaseUrl) || string.IsNullOrWhiteSpace(opts.InstanceId))
        {
            logger.LogDebug("Hub sync pominięty — integracja wyłączona lub niekompletna konfiguracja");
            return false;
        }

        return await SyncInstanceAsync(opts.InstanceId, cancellationToken: ct) is not null;
    }

    /// <summary>Reconciles every persisted HUB tenant plus the optional legacy bootstrap instance.</summary>
    public async Task<int> SyncAllAsync(CancellationToken cancellationToken = default)
    {
        var opts = Options;
        if (!opts.Enabled || string.IsNullOrWhiteSpace(opts.BaseUrl))
            return 0;

        try
        {
            using var scope = serviceProvider.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<WorkBaseDbContext>();
            var instanceIds = await db.Set<Tenant>()
                .AsNoTracking()
                .IgnoreQueryFilters()
                .Where(tenant => tenant.HubProductInstanceId != null)
                .Select(tenant => tenant.HubProductInstanceId!)
                .ToListAsync(cancellationToken);

            if (!string.IsNullOrWhiteSpace(opts.InstanceId))
                instanceIds.Add(opts.InstanceId);

            var synced = 0;
            foreach (var instanceId in instanceIds.Distinct(StringComparer.Ordinal))
            {
                if (await SyncInstanceAsync(instanceId, cancellationToken: cancellationToken) is not null)
                    synced++;
            }

            return synced;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Pełna rekoncyliacja instancji HUB nie powiodła się");
            return 0;
        }
    }

    /// <summary>Pobiera zweryfikowaną konfigurację jednej instancji produktu z HUB.</summary>
    public async Task<HubInstanceConfig?> GetInstanceConfigAsync(
        string instanceId,
        CancellationToken cancellationToken = default)
    {
        var opts = Options;
        if (!opts.Enabled || string.IsNullOrWhiteSpace(opts.BaseUrl) || string.IsNullOrWhiteSpace(instanceId))
            return null;

        try
        {
            var http = httpClientFactory.CreateClient("hub-platform");
            using var req = new HttpRequestMessage(
                HttpMethod.Get,
                $"{opts.BaseUrl.TrimEnd('/')}/api/v1/instances/{Uri.EscapeDataString(instanceId)}/config");
            req.Headers.Add("x-sso-client-id", opts.ClientId);
            req.Headers.Add("x-sso-secret", opts.ClientSecret);

            using var res = await http.SendAsync(req, cancellationToken);
            if (!res.IsSuccessStatusCode)
            {
                logger.LogWarning(
                    "Hub Entitlements API zwróciło {Status} dla instancji {InstanceId}",
                    (int)res.StatusCode, instanceId);
                return null;
            }

            var config = await res.Content.ReadFromJsonAsync<HubInstanceConfig>(cancellationToken: cancellationToken);
            if (config is null
                || !string.Equals(config.InstanceId, instanceId, StringComparison.Ordinal)
                || string.IsNullOrWhiteSpace(config.OrgId)
                || string.IsNullOrWhiteSpace(config.OrgSlug)
                || !string.Equals(config.ProductKey, opts.ClientId, StringComparison.OrdinalIgnoreCase))
            {
                logger.LogWarning(
                    "Hub zwrócił niespójną konfigurację instancji {InstanceId} (org={OrgId}, product={ProductKey})",
                    instanceId, config?.OrgId, config?.ProductKey);
                return null;
            }

            return config;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Nie udało się pobrać konfiguracji instancji HUB {InstanceId}", instanceId);
            return null;
        }
    }

    /// <summary>
    /// Zapewnia tenant dla organizacji danej instancji i nadpisuje jego FeatureFlags.
    /// Fail-soft: błąd HUB/provisioningu jest logowany i zwracany jako null.
    /// </summary>
    public async Task<HubTenantSyncResult?> SyncInstanceAsync(
        string instanceId,
        string? expectedOrganizationId = null,
        CancellationToken cancellationToken = default)
    {
        var config = await GetInstanceConfigAsync(instanceId, cancellationToken);
        if (config is null)
            return null;

        if (expectedOrganizationId is not null
            && !string.Equals(config.OrgId, expectedOrganizationId, StringComparison.Ordinal))
        {
            logger.LogWarning(
                "Instancja HUB {InstanceId} należy do organizacji {ActualOrgId}, a token wskazuje {ExpectedOrgId}",
                instanceId, config.OrgId, expectedOrganizationId);
            return null;
        }

        try
        {
            var opts = Options;
            using var scope = serviceProvider.CreateScope();
            var provisioning = scope.ServiceProvider.GetRequiredService<ITenantProvisioningService>();
            Guid? legacyTenantId = string.Equals(opts.InstanceId, config.InstanceId, StringComparison.Ordinal)
                                     && opts.TenantId != Guid.Empty
                ? opts.TenantId
                : null;

            var provisioned = await provisioning.EnsureHubTenantAsync(
                new HubTenantRegistration(
                    config.OrgId,
                    config.InstanceId,
                    config.OrganizationName ?? config.OrgName ?? config.OrgSlug,
                    config.OrgSlug,
                    legacyTenantId),
                cancellationToken);

            var db = scope.ServiceProvider.GetRequiredService<WorkBaseDbContext>();
            var tenant = await db.Set<Tenant>()
                .IgnoreQueryFilters()
                .SingleAsync(t => t.Id == provisioned.TenantId, cancellationToken);

            var accessEnabled = string.Equals(config.Status, "active", StringComparison.OrdinalIgnoreCase);
            if (accessEnabled) tenant.Activate();
            else tenant.Deactivate();

            var enabled = accessEnabled
                ? (config.Modules ?? []).ToHashSet(StringComparer.OrdinalIgnoreCase)
                : new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            // Nadpisujemy flagi 1:1 z odpowiedzią Huba dla WSZYSTKICH modułów z ModuleCatalog —
            // moduł nieobecny w odpowiedzi = wyłączony centralnie przez operatora.
            var flags = await db.Set<FeatureFlag>()
                .IgnoreQueryFilters()
                .Where(f => f.TenantId == provisioned.TenantId)
                .ToListAsync(cancellationToken);

            var changed = 0;
            foreach (var module in ModuleCatalog.All)
            {
                var shouldBeEnabled = enabled.Contains(module.Key);
                var flag = flags.FirstOrDefault(f => f.Module == module.Key);
                if (flag is null)
                {
                    db.Set<FeatureFlag>().Add(
                        FeatureFlag.Create(provisioned.TenantId, module.Key, shouldBeEnabled, "hub-sync"));
                    changed++;
                }
                else if (flag.IsEnabled != shouldBeEnabled)
                {
                    if (shouldBeEnabled) flag.Enable("hub-sync");
                    else flag.Disable();
                    changed++;
                }
            }

            await db.SaveChangesAsync(cancellationToken);
            scope.ServiceProvider.GetRequiredService<TenantAccessCache>()
                .Invalidate(provisioned.TenantId);

            logger.LogInformation(
                "Hub sync OK — tenant {TenantId}, org {OrgId}, plan {Plan}, status {Status}, modułów {Enabled}/{Total}, zmian {Changed}",
                provisioned.TenantId, config.OrgId, config.Plan, config.Status,
                enabled.Count, ModuleCatalog.All.Length, changed);

            return new HubTenantSyncResult(
                provisioned.TenantId,
                config.OrgId,
                config.InstanceId,
                config.Status,
                accessEnabled,
                provisioned.Created);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Hub sync nie powiódł się dla instancji {InstanceId}", instanceId);
            return null;
        }
    }
}

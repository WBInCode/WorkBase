using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using WorkBase.Infrastructure.Persistence;
using WorkBase.Modules.Identity.Domain.Entities;
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
    /// <summary>Tenant WorkBase, na który mapowana jest instancja Huba (dev: tenant seedowy).</summary>
    public Guid TenantId { get; init; }
}

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
    private sealed record InstanceConfigDto(
        string InstanceId,
        string OrgId,
        string OrgSlug,
        string ProductKey,
        string Status,
        string Plan,
        string[] Modules,
        string? CustomDomain);

    public HubOptions Options =>
        configuration.GetSection(HubOptions.SectionName).Get<HubOptions>() ?? new HubOptions();

    /// <summary>Pobiera config z Huba i nadpisuje FeatureFlags tenanta. Fail-soft: błędy tylko logujemy.</summary>
    public async Task<bool> SyncAsync(CancellationToken ct = default)
    {
        var opts = Options;
        if (!opts.Enabled || string.IsNullOrWhiteSpace(opts.BaseUrl) || string.IsNullOrWhiteSpace(opts.InstanceId))
        {
            logger.LogDebug("Hub sync pominięty — integracja wyłączona lub niekompletna konfiguracja");
            return false;
        }

        try
        {
            var http = httpClientFactory.CreateClient("hub-platform");
            using var req = new HttpRequestMessage(
                HttpMethod.Get,
                $"{opts.BaseUrl.TrimEnd('/')}/api/v1/instances/{opts.InstanceId}/config");
            req.Headers.Add("x-sso-client-id", opts.ClientId);
            req.Headers.Add("x-sso-secret", opts.ClientSecret);

            using var res = await http.SendAsync(req, ct);
            if (!res.IsSuccessStatusCode)
            {
                logger.LogWarning("Hub Entitlements API zwróciło {Status} — pomijam sync", (int)res.StatusCode);
                return false;
            }

            var config = await res.Content.ReadFromJsonAsync<InstanceConfigDto>(cancellationToken: ct);
            if (config is null) return false;

            var enabled = config.Modules.ToHashSet(StringComparer.OrdinalIgnoreCase);

            using var scope = serviceProvider.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<WorkBaseDbContext>();

            // Nadpisujemy flagi 1:1 z odpowiedzią Huba dla WSZYSTKICH modułów z ModuleCatalog —
            // moduł nieobecny w odpowiedzi = wyłączony centralnie przez operatora.
            var flags = await db.Set<FeatureFlag>()
                .IgnoreQueryFilters()
                .Where(f => f.TenantId == opts.TenantId)
                .ToListAsync(ct);

            var changed = 0;
            foreach (var module in ModuleCatalog.All)
            {
                var shouldBeEnabled = enabled.Contains(module.Key);
                var flag = flags.FirstOrDefault(f => f.Module == module.Key);
                if (flag is null)
                {
                    db.Set<FeatureFlag>().Add(FeatureFlag.Create(opts.TenantId, module.Key, shouldBeEnabled, "hub-sync"));
                    changed++;
                }
                else if (flag.IsEnabled != shouldBeEnabled)
                {
                    if (shouldBeEnabled) flag.Enable("hub-sync");
                    else flag.Disable();
                    changed++;
                }
            }

            if (changed > 0) await db.SaveChangesAsync(ct);

            logger.LogInformation(
                "Hub sync OK — plan {Plan}, status {Status}, modułów włączonych {Enabled}/{Total}, zmian {Changed}",
                config.Plan, config.Status, enabled.Count, ModuleCatalog.All.Length, changed);
            return true;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Hub sync nie powiódł się — zostają lokalne feature flags");
            return false;
        }
    }
}

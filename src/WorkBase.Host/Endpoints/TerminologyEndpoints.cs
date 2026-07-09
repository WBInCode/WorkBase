using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using WorkBase.Shared.Auth;
using WorkBase.Shared.Domain;

namespace WorkBase.Host.Endpoints;

/// <summary>
/// Per-tenant terminology overrides (white-label naming), e.g. renaming "Pracownik" to
/// "Konsultant". Stored via <see cref="ITenantConfigService"/> in cfg_tenant_configs under
/// the "terminology." key prefix and merged into the frontend's i18next resources after
/// login. See docs/AUDIT-KNOWLEDGE-MAP.md (module/branding/terminology configuration).
/// </summary>
public static class TerminologyEndpoints
{
    private const string KeyPrefix = "terminology.";
    private const int MaxKeyLength = 128;
    private const int MaxValueLength = 256;

    public static IEndpointRouteBuilder MapTerminologyEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/config/terminology").WithTags("Terminology").RequireAuthorization();

        // Readable by any authenticated user of the tenant — needed to render translated UI labels,
        // not just by admins.
        group.MapGet("/", async (ITenantConfigService config, HttpContext http) =>
        {
            var tenantId = http.User.GetTenantId();
            if (tenantId is null) return Results.Forbid();

            var all = await config.GetAllAsync(tenantId.Value, KeyPrefix);
            var overrides = all.ToDictionary(kv => kv.Key[KeyPrefix.Length..], kv => kv.Value);
            return Results.Ok(overrides);
        }).WithName("GetTerminology").WithSummary("Pobierz nadpisania nazewnictwa (i18n) dla tenanta");

        // Writable only by tenant admins.
        group.MapPut("/", async (UpdateTerminologyRequest request, ITenantConfigService config, HttpContext http) =>
        {
            var tenantId = http.User.GetTenantId();
            if (tenantId is null) return Results.Forbid();
            if (request.Overrides is null) return Results.BadRequest(new { message = "Brak danych." });

            foreach (var (key, value) in request.Overrides)
            {
                if (key.Length == 0 || key.Length > MaxKeyLength || !IsValidKey(key)) continue;

                var fullKey = KeyPrefix + key;
                if (string.IsNullOrWhiteSpace(value))
                    await config.DeleteAsync(tenantId.Value, fullKey);
                else
                    await config.SetAsync(tenantId.Value, fullKey, value.Length > MaxValueLength ? value[..MaxValueLength] : value);
            }

            return Results.NoContent();
        }).WithName("UpdateTerminology").WithSummary("Zapisz nadpisania nazewnictwa (i18n) dla tenanta").RequirePermission("config.manage");

        return endpoints;
    }

    // Restrict keys to i18next-style dotted identifiers (e.g. "nav.myDay") to keep the config
    // table free of arbitrary/oversized keys.
    private static bool IsValidKey(string key) =>
        key.All(c => char.IsLetterOrDigit(c) || c == '.' || c == '_');
}

public sealed record UpdateTerminologyRequest(Dictionary<string, string> Overrides);

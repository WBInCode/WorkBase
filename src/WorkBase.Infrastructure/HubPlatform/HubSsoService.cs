using System.Net.Http.Json;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;
using WorkBase.Shared.Auth;

namespace WorkBase.Infrastructure.HubPlatform;

/// <summary>Wyodrębnione roszczenia (claims) ze zweryfikowanego biletu handoff Huba.</summary>
public sealed record HandoffClaims(
    string Sub,
    string Email,
    string Name,
    string OrgId,
    string OrgRole,
    string InstanceId,
    string InstanceRole,
    string ProductKey,
    string[] Modules,
    string Jti);

/// <summary>
/// Weryfikacja i zużycie (redeem) biletu SSO wystawionego przez Hub ekosystemu.
/// Odpowiednik dziennik-v2 `HubService` — token weryfikowany lokalnie przez JWKS Huba
/// (bez współdzielonego sekretu), zużycie biletu chroni przed powtórnym użyciem (replay).
/// </summary>
public sealed class HubSsoService(
    IHttpClientFactory httpClientFactory,
    IConfiguration configuration,
    ILogger<HubSsoService> logger)
{
    private ConfigurationManager<OpenIdConnectConfiguration>? _configManager;

    private HubOptions Options =>
        configuration.GetSection(HubOptions.SectionName).Get<HubOptions>() ?? new HubOptions();

    private ConfigurationManager<OpenIdConnectConfiguration> GetConfigManager(HubOptions opts)
    {
        // Cache'owany per proces (ConfigurationManager sam odświeża klucze co ok. 24h) —
        // uniknięcie zapytania do Huba przy KAŻDYM handoff.
        return _configManager ??= new ConfigurationManager<OpenIdConnectConfiguration>(
            $"{opts.BaseUrl.TrimEnd('/')}/.well-known/openid-configuration",
            new OpenIdConnectConfigurationRetriever(),
            new HttpDocumentRetriever(httpClientFactory.CreateClient("hub-platform")) { RequireHttps = false });
    }

    /// <summary>Weryfikuje podpis (JWKS Huba), issuer, audience ("workbase") i typ ("handoff").</summary>
    public async Task<HandoffClaims> VerifyHandoffAsync(string token, CancellationToken ct = default)
    {
        var opts = Options;
        if (!opts.Enabled) throw new InvalidOperationException("Hub integration disabled");

        var config = await GetConfigManager(opts).GetConfigurationAsync(ct);
        var handler = new JwtSecurityTokenHandler();
        var principal = handler.ValidateToken(token, new TokenValidationParameters
        {
            ValidIssuer = opts.Issuer,
            ValidAudience = opts.ClientId, // audience huba = product_key ("workbase")
            IssuerSigningKeys = config.SigningKeys,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromSeconds(10),
        }, out var validatedToken);

        var jwt = (JwtSecurityToken)validatedToken;
        if (jwt.Claims.FirstOrDefault(c => c.Type == "typ")?.Value != "handoff")
            throw new SecurityTokenException("Not a handoff token");

        string Claim(string type) => principal.FindFirst(type)?.Value ?? "";
        var modulesClaim = jwt.Payload.TryGetValue("modules", out var m) && m is System.Collections.IEnumerable list
            ? list.Cast<object>().Select(x => x.ToString() ?? "").ToArray()
            : [];

        return new HandoffClaims(
            Sub: Claim("sub"),
            Email: Claim("email"),
            Name: Claim("name"),
            OrgId: Claim("org_id"),
            OrgRole: Claim("org_role"),
            InstanceId: Claim("instance_id"),
            InstanceRole: Claim("instance_role"),
            ProductKey: Claim("product_key"),
            Modules: modulesClaim,
            Jti: Claim("jti"));
    }

    /// <summary>Jednorazowe zużycie biletu server-to-server (ochrona przed replay) — jak entitlements sync.</summary>
    public async Task<bool> RedeemAsync(string token, CancellationToken ct = default)
    {
        var opts = Options;
        var http = httpClientFactory.CreateClient("hub-platform");
        using var req = new HttpRequestMessage(HttpMethod.Post, $"{opts.BaseUrl.TrimEnd('/')}/api/v1/sso/redeem");
        req.Headers.Add("x-sso-client-id", opts.ClientId);
        req.Headers.Add("x-sso-secret", opts.ClientSecret);
        req.Content = JsonContent.Create(new { token });

        using var res = await http.SendAsync(req, ct);
        if (!res.IsSuccessStatusCode)
        {
            logger.LogWarning("Redeem SSO nieudany: {Status}", res.StatusCode);
            return false;
        }
        return true;
    }

    /// <summary>Rola realm Keycloaka wynikająca z roli w Hubie (organizacja lub instancja).</summary>
    public static string MapRealmRole(string orgRole, string instanceRole)
    {
        return MapHubRole(orgRole, instanceRole) is "owner"
            ? "workbase-admin"
            : "workbase-user";
    }

    /// <summary>Canonical HUB role stored as a Keycloak attribute and mapped into tokens.</summary>
    public static string MapHubRole(string orgRole, string instanceRole)
    {
        var roles = new[] { orgRole, instanceRole }
            .Select(role => role.Trim().ToUpperInvariant())
            .ToHashSet(StringComparer.Ordinal);

        if (roles.Contains("OWNER")) return "owner";
        if (roles.Contains("ADMIN")) return "admin";
        return "member";
    }

    public static bool HasEligibleMembership(string orgRole, string instanceRole)
    {
        return new[] { orgRole, instanceRole }
            .Select(role => role.Trim().ToUpperInvariant())
            .Any(role => role is "OWNER" or "ADMIN" or "MEMBER");
    }

    public static bool RequiresEmployeeRecord(string canonicalHubRole) =>
        !string.Equals(canonicalHubRole, "owner", StringComparison.OrdinalIgnoreCase);

    /// <summary>Maps a canonical HUB role to a tenant-local WorkBase role.</summary>
    public static string? MapApplicationRole(string? hubRole, Guid tenantId)
    {
        return hubRole?.Trim().ToLowerInvariant() switch
        {
            "owner" when tenantId == PlatformConstants.OperatorTenantId => "Super Admin",
            "owner" => "Admin",
            "admin" or "member" => "Pracownik",
            _ => null,
        };
    }
}

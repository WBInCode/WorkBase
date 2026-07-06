using System.Collections.Concurrent;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;

namespace WorkBase.Infrastructure.Auth.MultiRealm;

/// <summary>
/// Synchronous IssuerValidator + IssuerSigningKeyResolver for multi-realm Keycloak, backed by
/// <see cref="TenantIssuerCache"/>. Only used when Keycloak:MultiRealmEnabled=true (see
/// AuthenticationExtensions) — single-realm deployments keep using the framework's built-in
/// Authority-based validation, completely untouched.
///
/// THREAT MODEL: an attacker can put ANY string in a JWT's "iss" claim. Both delegates below
/// treat the cache (built independently from our own Tenant table, never from the token
/// itself) as the sole source of truth for "is this issuer one of ours" — an issuer that
/// doesn't match a cached entry is rejected immediately, with no network call ever made for
/// it. This prevents both token-confusion attacks (accepting a token minted by a realm that
/// isn't actually provisioned for a tenant) and SSRF-via-issuer (fetching JWKS from an
/// attacker-controlled URL).
///
/// STILL REQUIRES SECURITY REVIEW before enabling in production: in particular, confirm (1)
/// Keycloak:Audience / client_id is identical across all tenant realms so ValidAudience checks
/// remain meaningful, (2) the 5-minute cache refresh interval is acceptable for realm
/// suspension/removal to take effect, (3) rate/size limits on the underlying
/// ConfigurationManager HTTP calls are acceptable for the expected number of tenant realms.
/// </summary>
public sealed class DynamicIssuerValidation(TenantIssuerCache cache)
{
    // One ConfigurationManager (JWKS fetch + cache, refreshed per Microsoft's own internal
    // logic) per known-valid issuer. Never created for an issuer that isn't already in the
    // TenantIssuerCache — see ResolveSigningKeys below.
    private readonly ConcurrentDictionary<string, ConfigurationManager<OpenIdConnectConfiguration>> _configManagers = new(StringComparer.Ordinal);

    public string ValidateIssuer(string issuer, SecurityToken token, TokenValidationParameters parameters)
    {
        if (!cache.IsValidIssuer(issuer))
        {
            throw new SecurityTokenInvalidIssuerException($"Issuer '{issuer}' is not a known WorkBase tenant realm.")
            {
                InvalidIssuer = issuer
            };
        }

        return issuer;
    }

    public IEnumerable<SecurityKey> ResolveSigningKeys(
        string token,
        SecurityToken securityToken,
        string kid,
        TokenValidationParameters validationParameters)
    {
        var issuer = ExtractIssuer(securityToken);

        if (issuer is null || !cache.IsValidIssuer(issuer))
            return [];

        var configManager = _configManagers.GetOrAdd(issuer, iss => new ConfigurationManager<OpenIdConnectConfiguration>(
            $"{iss}/.well-known/openid-configuration",
            new OpenIdConnectConfigurationRetriever(),
            new HttpDocumentRetriever { RequireHttps = validationParameters.RequireSignedTokens }));

        // GetConfigurationAsync internally caches (per ConfigurationManager) and only performs
        // an actual HTTP refresh when its own cache has expired — blocking here is the
        // standard, documented pattern for multi-tenant JWKS resolution with this API, since
        // IssuerSigningKeyResolver has no async overload.
        var config = configManager.GetConfigurationAsync().GetAwaiter().GetResult();
        return config.SigningKeys;
    }

    private static string? ExtractIssuer(SecurityToken securityToken) => securityToken switch
    {
        System.IdentityModel.Tokens.Jwt.JwtSecurityToken jwt => jwt.Issuer,
        Microsoft.IdentityModel.JsonWebTokens.JsonWebToken jwt => jwt.Issuer,
        _ => null
    };
}

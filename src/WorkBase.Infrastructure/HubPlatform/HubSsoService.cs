using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Crypto.Signers;
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
    string? EmployeeReference,
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
    private static readonly TimeSpan JwksLifetime = TimeSpan.FromHours(1);
    private static readonly TimeSpan ClockSkew = TimeSpan.FromSeconds(10);
    private readonly SemaphoreSlim _jwksLock = new(1, 1);
    private IReadOnlyDictionary<string, byte[]> _signingKeys = new Dictionary<string, byte[]>();
    private DateTimeOffset _signingKeysExpiresAt;

    private sealed record JwksResponse([property: JsonPropertyName("keys")] Jwk[] Keys);

    private sealed record Jwk(
        [property: JsonPropertyName("kid")] string Kid,
        [property: JsonPropertyName("kty")] string Kty,
        [property: JsonPropertyName("crv")] string Crv,
        [property: JsonPropertyName("x")] string X,
        [property: JsonPropertyName("alg")] string? Alg,
        [property: JsonPropertyName("use")] string? Use);

    private HubOptions Options =>
        configuration.GetSection(HubOptions.SectionName).Get<HubOptions>() ?? new HubOptions();

    /// <summary>Weryfikuje podpis (JWKS Huba), issuer, audience ("workbase") i typ ("handoff").</summary>
    public async Task<HandoffClaims> VerifyHandoffAsync(string token, CancellationToken ct = default)
    {
        var opts = Options;
        if (!opts.Enabled) throw new InvalidOperationException("Hub integration disabled");

        if (string.IsNullOrWhiteSpace(token) || token.Length > 16_384)
            throw new SecurityTokenException("Malformed handoff token");
        var parts = token.Split('.');
        if (parts.Length != 3)
            throw new SecurityTokenException("Malformed handoff token");

        using var header = ParseSegment(parts[0], "header");
        using var payload = ParseSegment(parts[1], "payload");
        EnsureUniqueProperties(header.RootElement);
        EnsureUniqueProperties(payload.RootElement);

        var algorithm = RequiredString(header.RootElement, "alg");
        var keyId = RequiredString(header.RootElement, "kid");
        if (algorithm != "EdDSA")
            throw new SecurityTokenInvalidAlgorithmException($"Unexpected HUB JWT algorithm '{algorithm}'.");

        var publicKey = await GetSigningKeyAsync(opts, keyId, forceRefresh: false, ct)
            ?? await GetSigningKeyAsync(opts, keyId, forceRefresh: true, ct)
            ?? throw new SecurityTokenSignatureKeyNotFoundException($"HUB signing key '{keyId}' was not found.");
        var signature = DecodeSegment(parts[2], "signature");
        if (signature.Length != 64)
            throw new SecurityTokenInvalidSignatureException("Invalid Ed25519 signature length.");

        var signingInput = Encoding.ASCII.GetBytes($"{parts[0]}.{parts[1]}");
        var verifier = new Ed25519Signer();
        verifier.Init(false, new Ed25519PublicKeyParameters(publicKey, 0));
        verifier.BlockUpdate(signingInput, 0, signingInput.Length);
        if (!verifier.VerifySignature(signature))
            throw new SecurityTokenInvalidSignatureException("HUB handoff signature is invalid.");

        ValidateClaims(payload.RootElement, opts);
        var modules = payload.RootElement.TryGetProperty("modules", out var modulesElement)
                      && modulesElement.ValueKind == JsonValueKind.Array
            ? modulesElement.EnumerateArray()
                .Select(item => item.ValueKind == JsonValueKind.String
                    ? item.GetString()!
                    : throw new SecurityTokenException("Invalid modules claim."))
                .ToArray()
            : [];

        return new HandoffClaims(
            Sub: RequiredString(payload.RootElement, "sub"),
            Email: RequiredString(payload.RootElement, "email"),
            Name: RequiredString(payload.RootElement, "name"),
            OrgId: RequiredString(payload.RootElement, "org_id"),
            OrgRole: RequiredString(payload.RootElement, "org_role"),
            InstanceId: RequiredString(payload.RootElement, "instance_id"),
            InstanceRole: RequiredString(payload.RootElement, "instance_role"),
            ProductKey: RequiredString(payload.RootElement, "product_key"),
            EmployeeReference: OptionalString(payload.RootElement, "employee_ref"),
            Modules: modules,
            Jti: RequiredString(payload.RootElement, "jti"));
    }

    private async Task<byte[]?> GetSigningKeyAsync(
        HubOptions options,
        string keyId,
        bool forceRefresh,
        CancellationToken cancellationToken)
    {
        if (!forceRefresh
            && DateTimeOffset.UtcNow < _signingKeysExpiresAt
            && _signingKeys.TryGetValue(keyId, out var cached))
        {
            return cached;
        }

        await _jwksLock.WaitAsync(cancellationToken);
        try
        {
            if (!forceRefresh
                && DateTimeOffset.UtcNow < _signingKeysExpiresAt
                && _signingKeys.TryGetValue(keyId, out cached))
            {
                return cached;
            }

            var url = $"{options.BaseUrl.TrimEnd('/')}/.well-known/jwks.json";
            var jwks = await httpClientFactory.CreateClient("hub-platform")
                .GetFromJsonAsync<JwksResponse>(url, cancellationToken)
                ?? throw new SecurityTokenSignatureKeyNotFoundException("HUB JWKS response was empty.");
            var keys = new Dictionary<string, byte[]>(StringComparer.Ordinal);
            foreach (var key in jwks.Keys)
            {
                if (key.Kty != "OKP"
                    || key.Crv != "Ed25519"
                    || (key.Alg is not null && key.Alg != "EdDSA")
                    || (key.Use is not null && key.Use != "sig")
                    || string.IsNullOrWhiteSpace(key.Kid))
                {
                    continue;
                }

                var encoded = DecodeSegment(key.X, "JWK x");
                if (encoded.Length == Ed25519PublicKeyParameters.KeySize)
                    keys[key.Kid] = encoded;
            }

            _signingKeys = keys;
            _signingKeysExpiresAt = DateTimeOffset.UtcNow.Add(JwksLifetime);
            return keys.GetValueOrDefault(keyId);
        }
        finally
        {
            _jwksLock.Release();
        }
    }

    private static JsonDocument ParseSegment(string segment, string name)
    {
        try
        {
            return JsonDocument.Parse(DecodeSegment(segment, name));
        }
        catch (Exception ex) when (ex is FormatException or JsonException)
        {
            throw new SecurityTokenException($"Malformed JWT {name}.", ex);
        }
    }

    private static byte[] DecodeSegment(string value, string name)
    {
        try
        {
            return Base64UrlEncoder.DecodeBytes(value);
        }
        catch (FormatException ex)
        {
            throw new SecurityTokenException($"Malformed base64url {name}.", ex);
        }
    }

    private static void EnsureUniqueProperties(JsonElement element)
    {
        if (element.ValueKind != JsonValueKind.Object)
            throw new SecurityTokenException("JWT segment must be a JSON object.");

        var names = new HashSet<string>(StringComparer.Ordinal);
        foreach (var property in element.EnumerateObject())
        {
            if (!names.Add(property.Name))
                throw new SecurityTokenException($"Duplicate JWT claim '{property.Name}'.");
        }
    }

    private static string RequiredString(JsonElement payload, string name)
    {
        if (!payload.TryGetProperty(name, out var value)
            || value.ValueKind != JsonValueKind.String
            || string.IsNullOrWhiteSpace(value.GetString()))
        {
            throw new SecurityTokenException($"Required JWT claim '{name}' is missing.");
        }

        return value.GetString()!;
    }

    private static string? OptionalString(JsonElement payload, string name)
    {
        if (!payload.TryGetProperty(name, out var value) || value.ValueKind == JsonValueKind.Null)
            return null;
        if (value.ValueKind != JsonValueKind.String)
            throw new SecurityTokenException($"Optional JWT claim '{name}' is invalid.");

        var result = value.GetString()?.Trim();
        if (result?.Length > 128)
            throw new SecurityTokenException($"Optional JWT claim '{name}' is too long.");
        return string.IsNullOrEmpty(result) ? null : result;
    }

    private static long RequiredNumericDate(JsonElement payload, string name)
    {
        if (!payload.TryGetProperty(name, out var value)
            || value.ValueKind != JsonValueKind.Number
            || !value.TryGetInt64(out var result))
        {
            throw new SecurityTokenException($"Required JWT claim '{name}' is invalid.");
        }

        return result;
    }

    private static void ValidateClaims(JsonElement payload, HubOptions options)
    {
        if (RequiredString(payload, "typ") != "handoff")
            throw new SecurityTokenException("Not a handoff token.");
        if (RequiredString(payload, "iss") != options.Issuer)
            throw new SecurityTokenInvalidIssuerException("HUB token issuer is invalid.");

        if (!payload.TryGetProperty("aud", out var audience)
            || !AudienceContains(audience, options.ClientId))
        {
            throw new SecurityTokenInvalidAudienceException("HUB token audience is invalid.");
        }

        var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var skew = (long)ClockSkew.TotalSeconds;
        var expiresAt = RequiredNumericDate(payload, "exp");
        if (expiresAt <= now - skew)
            throw new SecurityTokenExpiredException("HUB handoff token has expired.");
        if (payload.TryGetProperty("nbf", out var notBefore))
        {
            if (notBefore.ValueKind != JsonValueKind.Number
                || !notBefore.TryGetInt64(out var nbf)
                || nbf > now + skew)
            {
                throw new SecurityTokenNotYetValidException("HUB handoff token is not yet valid.");
            }
        }

        if (payload.TryGetProperty("iat", out var issuedAt)
            && (issuedAt.ValueKind != JsonValueKind.Number
                || !issuedAt.TryGetInt64(out var iat)
                || iat > now + skew))
        {
            throw new SecurityTokenException("HUB handoff issued-at claim is invalid.");
        }

        _ = RequiredString(payload, "jti");
        _ = RequiredString(payload, "sub");
        _ = RequiredString(payload, "email");
        _ = RequiredString(payload, "name");
        _ = RequiredString(payload, "org_id");
        _ = RequiredString(payload, "org_role");
        _ = RequiredString(payload, "instance_id");
        _ = RequiredString(payload, "instance_role");
        _ = RequiredString(payload, "product_key");
    }

    private static bool AudienceContains(JsonElement audience, string expected)
    {
        return audience.ValueKind switch
        {
            JsonValueKind.String => audience.GetString() == expected,
            JsonValueKind.Array => audience.EnumerateArray().Any(item =>
                item.ValueKind == JsonValueKind.String && item.GetString() == expected),
            _ => false,
        };
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

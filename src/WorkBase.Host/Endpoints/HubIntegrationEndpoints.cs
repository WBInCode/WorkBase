using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using WorkBase.Contracts;
using WorkBase.Infrastructure.HubPlatform;

namespace WorkBase.Host.Endpoints;

/// <summary>
/// Endpoint webhooków Huba ekosystemu (wb-platform). Hub podpisuje ładunek
/// HMAC-SHA256 (nagłówek <c>x-wb-signature</c>: "sha256=&lt;hex&gt;") — podpis
/// jest jedyną autoryzacją (endpoint anonimowy, jak w dziennik-v2/chatv2).
/// Obsługiwane zdarzenia: <c>entitlements.updated</c> → resync feature flags.
/// </summary>
public static class HubIntegrationEndpoints
{
    public static IEndpointRouteBuilder MapHubIntegrationEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/hub").WithTags("HubPlatform");

        group.MapPost("/webhooks", async (
            HttpContext http,
            HubEntitlementsSyncService sync,
            ILoggerFactory loggerFactory) =>
        {
            var logger = loggerFactory.CreateLogger("HubWebhooks");
            var opts = sync.Options;
            if (!opts.Enabled) return Results.NotFound();

            // Weryfikacja HMAC na surowym body
            http.Request.EnableBuffering();
            using var reader = new StreamReader(http.Request.Body, Encoding.UTF8, leaveOpen: true);
            var rawBody = await reader.ReadToEndAsync();
            http.Request.Body.Position = 0;

            var signature = http.Request.Headers["x-wb-signature"].ToString();
            if (string.IsNullOrEmpty(signature) || !VerifySignature(opts.WebhookSecret, rawBody, signature))
            {
                logger.LogWarning("Webhook Huba odrzucony — nieprawidłowy podpis");
                return Results.Unauthorized();
            }

            var eventName = http.Request.Headers["x-wb-event"].ToString();
            logger.LogInformation("Webhook Huba: {Event}", eventName);

            if (eventName is "entitlements.updated")
            {
                // Fire-and-forget z perspektywy Huba — odpowiadamy szybko 200,
                // sync robimy synchronicznie (lekki: 1 GET + kilka UPDATE).
                await sync.SyncAsync(http.RequestAborted);
            }
            // session.revoked: sesje WorkBase żyją w Keycloak — rewokacja SSO
            // dotyczy sesji Huba; lokalne tokeny wygasają naturalnie (krótki TTL).

            return Results.Ok(new { ok = true });
        })
        .AllowAnonymous()
        .WithName("HubWebhooks")
        .WithSummary("Webhook Huba ekosystemu (entitlements.updated — podpis HMAC)");

        group.MapPost("/sync", async (HubEntitlementsSyncService sync, CancellationToken ct) =>
        {
            var ok = await sync.SyncAsync(ct);
            return ok ? Results.Ok(new { synced = true }) : Results.Ok(new { synced = false });
        })
        .RequireAuthorization()
        .WithName("HubManualSync")
        .WithSummary("Ręczny resync modułów z Hub Entitlements API");

        // Poza grupą /api/hub — Hub konstruuje URL callbacku jako {baseUrl}/sso/callback
        // (zgodnie z innymi produktami: dziennik-v2, chatv2). nginx musi go proxować do API
        // (patrz frontend/nginx.conf — dopasowanie DOKŁADNE, żeby nie połknąć /auth/sso-bridge SPA).
        endpoints.MapGet("/sso/callback", async (
            string? token,
            HubSsoService sso,
            HubEntitlementsSyncService entitlements,
            IKeycloakAdminService keycloak,
            IConfiguration configuration,
            ILoggerFactory loggerFactory,
            CancellationToken ct) =>
        {
            var logger = loggerFactory.CreateLogger("HubSsoCallback");
            var opts = entitlements.Options;
            if (!opts.Enabled) return Results.NotFound();

            var frontendUrl = opts.FrontendUrl.TrimEnd('/');
            Microsoft.AspNetCore.Http.IResult RedirectError(string code) =>
                Results.Redirect($"{frontendUrl}/login?sso_error={code}");

            if (string.IsNullOrEmpty(token)) return RedirectError("missing_token");

            HandoffClaims claims;
            try
            {
                claims = await sso.VerifyHandoffAsync(token, ct);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Odrzucony handoff SSO");
                return RedirectError("invalid");
            }

            if (claims.InstanceId != opts.InstanceId) return RedirectError("wrong_instance");

            if (!await sso.RedeemAsync(token, ct)) return RedirectError("used");

            // JIT provisioning: konto w Keycloaku powiązane po e-mailu, rola zsynchronizowana
            // z Hubem przy KAŻDYM logowaniu (CreateUserInRealmAsync jest idempotentny — 409 =
            // już istnieje, wtedy tylko dogrywa role realm).
            var realm = configuration["Keycloak:Realm"] ?? "workbase";
            var nameParts = claims.Name.Split(' ', 2);
            var role = HubSsoService.MapRealmRole(claims.OrgRole, claims.InstanceRole);
            await keycloak.CreateUserInRealmAsync(
                realm,
                claims.Email,
                nameParts.ElementAtOrDefault(0) ?? claims.Email,
                nameParts.ElementAtOrDefault(1) ?? "",
                temporaryPassword: null,
                attributes: new Dictionary<string, string> { ["hub_org_id"] = claims.OrgId },
                realmRoles: [role],
                cancellationToken: ct);

            // Frontend inicjuje PRAWDZIWY Authorization Code + PKCE (react-oidc-context) —
            // e-mail tylko podpowiada Keycloakowi kogo logujemy (login_hint), nic wrażliwego
            // nie leci w URL. Konto już istnieje i ma właściwą rolę, więc to już tylko hasło.
            return Results.Redirect($"{frontendUrl}/auth/sso-bridge?email={Uri.EscapeDataString(claims.Email)}");
        })
        .AllowAnonymous()
        .WithName("HubSsoCallback")
        .WithSummary("Handoff SSO z Huba: weryfikacja + JIT provisioning konta Keycloak, potem redirect do logowania z podpowiedzią e-maila");

        return endpoints;
    }

    private static bool VerifySignature(string secret, string body, string signature)
    {
        if (string.IsNullOrEmpty(secret)) return false;
        var expected = "sha256=" + Convert.ToHexString(
            HMACSHA256.HashData(Encoding.UTF8.GetBytes(secret), Encoding.UTF8.GetBytes(body)))
            .ToLowerInvariant();
        return CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(expected),
            Encoding.UTF8.GetBytes(signature));
    }
}

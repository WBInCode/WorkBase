using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;
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

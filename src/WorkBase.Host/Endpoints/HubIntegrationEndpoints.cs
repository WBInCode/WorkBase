using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using WorkBase.Contracts;
using WorkBase.Infrastructure.HubPlatform;
using WorkBase.Shared.Auth;

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
                var instanceId = GetWebhookInstanceId(http.Request, rawBody);
                var synced = instanceId is not null
                    ? await sync.SyncInstanceAsync(instanceId, cancellationToken: http.RequestAborted) is not null
                    : await sync.SyncAllAsync(http.RequestAborted) > 0;

                if (!synced)
                {
                    logger.LogWarning("Webhook Huba nie wskazał instancji możliwej do synchronizacji");
                    return Results.StatusCode(StatusCodes.Status503ServiceUnavailable);
                }
            }
            // session.revoked: sesje WorkBase żyją w Keycloak — rewokacja SSO
            // dotyczy sesji Huba; lokalne tokeny wygasają naturalnie (krótki TTL).

            return Results.Ok(new { ok = true });
        })
        .AllowAnonymous()
        .WithName("HubWebhooks")
        .WithSummary("Webhook Huba ekosystemu (entitlements.updated — podpis HMAC)");

        group.MapPost("/sync", async (string? instanceId, HubEntitlementsSyncService sync, CancellationToken ct) =>
        {
            var ok = string.IsNullOrWhiteSpace(instanceId)
                ? await sync.SyncAllAsync(ct) > 0
                : await sync.SyncInstanceAsync(instanceId, cancellationToken: ct) is not null;
            return ok ? Results.Ok(new { synced = true }) : Results.Ok(new { synced = false });
        })
        .RequirePlatformOperator()
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
            IHubEmployeeIdentityLinker employeeIdentityLinker,
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

            if (string.IsNullOrWhiteSpace(claims.OrgId)
                || string.IsNullOrWhiteSpace(claims.InstanceId)
                || !HubSsoService.HasEligibleMembership(claims.OrgRole, claims.InstanceRole)
                || !string.Equals(claims.ProductKey, opts.ClientId, StringComparison.OrdinalIgnoreCase))
            {
                return RedirectError("wrong_instance");
            }

            if (!await sso.RedeemAsync(token, ct)) return RedirectError("used");

            var tenantSync = await entitlements.SyncInstanceAsync(claims.InstanceId, claims.OrgId, ct);
            if (tenantSync is null) return RedirectError("wrong_instance");
            if (!tenantSync.AccessEnabled) return RedirectError("access_inactive");

            var employeeDecision = await employeeIdentityLinker.ResolveForSsoAsync(
                tenantSync.TenantId, claims.Email, ct);
            if (employeeDecision.AccessDenied) return RedirectError("employee_inactive");
            var hubRole = HubSsoService.MapHubRole(claims.OrgRole, claims.InstanceRole);
            if (!employeeDecision.EmployeeId.HasValue && HubSsoService.RequiresEmployeeRecord(hubRole))
                return RedirectError("employee_missing");

            // JIT provisioning: konto w Keycloaku powiązane po e-mailu, rola zsynchronizowana
            // z Hubem przy KAŻDYM logowaniu (CreateUserInRealmAsync jest idempotentny — 409 =
            // już istnieje, wtedy tylko dogrywa role realm).
            var realm = configuration["Keycloak:Realm"] ?? "workbase";
            var nameParts = claims.Name.Split(' ', 2);
            var role = HubSsoService.MapRealmRole(claims.OrgRole, claims.InstanceRole);
            var attributes = new Dictionary<string, string>
            {
                ["tenant_id"] = tenantSync.TenantId.ToString(),
                ["hub_org_id"] = claims.OrgId,
                ["hub_instance_id"] = claims.InstanceId,
                ["hub_user_id"] = claims.Sub,
                ["hub_role"] = hubRole,
            };
            var keycloakUserId = await keycloak.CreateUserInRealmAsync(
                realm,
                claims.Email,
                nameParts.ElementAtOrDefault(0) ?? claims.Email,
                nameParts.ElementAtOrDefault(1) ?? "",
                temporaryPassword: null,
                attributes,
                realmRoles: [role],
                cancellationToken: ct);
            if (keycloakUserId is null) return RedirectError("account_provisioning");

            if (employeeDecision.EmployeeId.HasValue)
            {
                var linked = await employeeIdentityLinker.LinkOnSsoAsync(
                    tenantSync.TenantId,
                    employeeDecision.EmployeeId.Value,
                    claims.Sub,
                    keycloakUserId,
                    ct);
                if (!linked) return RedirectError("account_linking");
                attributes["employee_id"] = employeeDecision.EmployeeId.Value.ToString();
            }

            // CreateUserInRealmAsync is idempotent; on an existing account Keycloak returns
            // 409, so explicitly refresh the HUB-managed attributes on every handoff.
            await keycloak.SetUserAttributesAsync(keycloakUserId, attributes, ct);
            await keycloak.SyncUserRealmRolesAsync(
                realm,
                keycloakUserId,
                managedRoleNames: ["workbase-admin", "workbase-user"],
                assignedRoleNames: [role],
                cancellationToken: ct);

            // Frontend inicjuje PRAWDZIWY Authorization Code + PKCE (react-oidc-context) —
            // e-mail tylko podpowiada Keycloakowi kogo logujemy (login_hint), nic wrażliwego
            // nie leci w URL. Konto już istnieje i ma właściwą rolę, więc to już tylko hasło.
            return Results.Redirect(
                $"{frontendUrl}/auth/sso-bridge?realm=&email={Uri.EscapeDataString(claims.Email)}");
        })
        .AllowAnonymous()
        .WithName("HubSsoCallback")
        .WithSummary("Handoff SSO z Huba: weryfikacja + JIT provisioning konta Keycloak, potem redirect do logowania z podpowiedzią e-maila");

        return endpoints;
    }

    private static string? GetWebhookInstanceId(HttpRequest request, string rawBody)
    {
        var headerValue = request.Headers["x-wb-instance-id"].ToString();
        if (!string.IsNullOrWhiteSpace(headerValue))
            return headerValue;

        try
        {
            using var document = JsonDocument.Parse(rawBody);
            return FindInstanceId(document.RootElement, depth: 0);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static string? FindInstanceId(JsonElement element, int depth)
    {
        if (depth > 10)
            return null;

        if (element.ValueKind == JsonValueKind.Object)
        {
            foreach (var property in element.EnumerateObject())
            {
                if ((property.NameEquals("instanceId") || property.NameEquals("instance_id"))
                    && property.Value.ValueKind == JsonValueKind.String)
                {
                    return property.Value.GetString();
                }

                var nested = FindInstanceId(property.Value, depth + 1);
                if (nested is not null) return nested;
            }
        }
        else if (element.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in element.EnumerateArray())
            {
                var nested = FindInstanceId(item, depth + 1);
                if (nested is not null) return nested;
            }
        }

        return null;
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

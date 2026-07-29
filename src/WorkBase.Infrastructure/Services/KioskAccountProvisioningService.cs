using System.Net;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using WorkBase.Contracts;
using WorkBase.Infrastructure.HubPlatform;
using WorkBase.Infrastructure.Persistence;
using WorkBase.Modules.Organization.Domain.Entities;

namespace WorkBase.Infrastructure.Services;

public sealed class KioskAccountProvisioningService(
    WorkBaseDbContext dbContext,
    IKeycloakAdminService keycloakAdmin,
    IEmailSender emailSender,
    IHttpClientFactory httpClientFactory,
    IConfiguration configuration,
    ILogger<KioskAccountProvisioningService> logger) : IKioskAccountProvisioningService
{
    public async Task<KioskAccountProvisioningResult?> EnsureForTenantAsync(
        Guid tenantId,
        string administratorEmail,
        bool credentialsCanBeReturned,
        CancellationToken cancellationToken = default)
    {
        if (!dbContext.Database.IsRelational())
        {
            return await EnsureCoreAsync(
                tenantId, administratorEmail, credentialsCanBeReturned, cancellationToken);
        }

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        await dbContext.Database.ExecuteSqlInterpolatedAsync(
            $"SELECT pg_advisory_xact_lock(hashtextextended({$"tenant-kiosk:{tenantId}"}, 0))",
            cancellationToken);
        var result = await EnsureCoreAsync(
            tenantId, administratorEmail, credentialsCanBeReturned, cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return result;
    }

    private async Task<KioskAccountProvisioningResult?> EnsureCoreAsync(
        Guid tenantId,
        string administratorEmail,
        bool credentialsCanBeReturned,
        CancellationToken cancellationToken)
    {
        var tenant = await dbContext.Set<Tenant>()
            .AsNoTracking()
            .IgnoreQueryFilters()
            .SingleOrDefaultAsync(item => item.Id == tenantId, cancellationToken);
        if (tenant is null)
            return null;

        var realmName = tenant.KeycloakRealmName
            ?? configuration["Keycloak:Realm"]
            ?? "workbase";
        var username = BuildUsername(tenant.Slug, tenant.Id);
        var temporaryPassword = GenerateTemporaryPassword();
        var prepared = await keycloakAdmin.PrepareKioskUserAsync(
            realmName,
            username,
            tenant.Name,
            temporaryPassword,
            new Dictionary<string, string>
            {
                ["tenant_id"] = tenant.Id.ToString(),
                ["kiosk_location"] = "Glowna",
                ["kiosk_managed"] = "true",
            },
            cancellationToken);
        if (prepared is null)
            return null;

        var loginUrl = BuildLoginUrl(tenant.KeycloakRealmName);
        if (!prepared.CredentialsIssued)
        {
            return new KioskAccountProvisioningResult(
                username,
                loginUrl,
                TemporaryPassword: null,
                CredentialsEmailSent: false,
                CredentialsDelivered: true);
        }

        var emailSent = await TrySendCredentialsAsync(
            administratorEmail,
            tenant.Name,
            tenant.HubProductInstanceId,
            username,
            temporaryPassword,
            loginUrl,
            cancellationToken);
        var deliveredToAdministrator = emailSent || credentialsCanBeReturned;
        var deliveryRecorded = false;
        if (deliveredToAdministrator)
        {
            deliveryRecorded = await keycloakAdmin.MarkKioskCredentialsDeliveredAsync(
                realmName,
                prepared.UserId,
                cancellationToken);
        }

        return new KioskAccountProvisioningResult(
            username,
            loginUrl,
            credentialsCanBeReturned ? temporaryPassword : null,
            emailSent,
            deliveredToAdministrator && deliveryRecorded);
    }

    private async Task<bool> TrySendCredentialsAsync(
        string administratorEmail,
        string companyName,
        string? hubProductInstanceId,
        string username,
        string temporaryPassword,
        string loginUrl,
        CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(hubProductInstanceId)
            && await TrySendViaHubAsync(
                hubProductInstanceId,
                administratorEmail,
                username,
                temporaryPassword,
                loginUrl,
                cancellationToken))
        {
            return true;
        }

        var safeCompanyName = WebUtility.HtmlEncode(companyName);
        var safeUsername = WebUtility.HtmlEncode(username);
        var safePassword = WebUtility.HtmlEncode(temporaryPassword);
        var safeLoginUrl = WebUtility.HtmlEncode(loginUrl);
        var body = $$"""
            <h2>WorkBase Kiosk jest gotowy</h2>
            <p>Dla firmy <strong>{{safeCompanyName}}</strong> utworzono konto terminala rejestracji czasu pracy.</p>
            <p><strong>Adres:</strong> <a href="{{safeLoginUrl}}">{{safeLoginUrl}}</a><br>
            <strong>Login:</strong> <code>{{safeUsername}}</code><br>
            <strong>Haslo tymczasowe:</strong> <code>{{safePassword}}</code></p>
            <p>Przy pierwszym logowaniu Keycloak poprosi o ustawienie nowego hasla. Na ekranie kiosku mozna zainstalowac aplikacje PWA w Edge lub Chrome. Na telefonie po instalacji nalezy dodatkowo wlaczyc przypiecie ekranu (Android) albo Dostep nadzorowany (iOS).</p>
            <p>Nie przekazuj danych konta terminala pracownikom.</p>
            """;

        try
        {
            await emailSender.SendAsync(
                administratorEmail,
                $"WorkBase Kiosk - dane startowe dla {companyName}",
                body,
                cancellationToken);
            return true;
        }
        catch (Exception exception) when (!cancellationToken.IsCancellationRequested)
        {
            logger.LogWarning(
                exception,
                "Kiosk credentials could not be emailed to the tenant administrator");
            return false;
        }
    }

    private async Task<bool> TrySendViaHubAsync(
        string productInstanceId,
        string administratorEmail,
        string username,
        string temporaryPassword,
        string loginUrl,
        CancellationToken cancellationToken)
    {
        var options = configuration.GetSection(HubOptions.SectionName).Get<HubOptions>() ?? new HubOptions();
        if (!options.Enabled
            || string.IsNullOrWhiteSpace(options.BaseUrl)
            || string.IsNullOrWhiteSpace(options.ClientId)
            || string.IsNullOrWhiteSpace(options.ClientSecret))
        {
            return false;
        }

        try
        {
            using var request = new HttpRequestMessage(
                HttpMethod.Post,
                $"{options.BaseUrl.TrimEnd('/')}/api/v1/instances/{Uri.EscapeDataString(productInstanceId)}/kiosk-credentials");
            request.Headers.Add("x-sso-client-id", options.ClientId);
            request.Headers.Add("x-sso-secret", options.ClientSecret);
            request.Headers.Add(
                "idempotency-key",
                Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(temporaryPassword)))
                    .ToLowerInvariant());
            request.Content = JsonContent.Create(new
            {
                email = administratorEmail,
                username,
                temporaryPassword,
                loginUrl,
            });
            using var response = await httpClientFactory.CreateClient("hub-platform")
                .SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning(
                    "Hub could not deliver kiosk credentials: {Status}",
                    (int)response.StatusCode);
                return false;
            }

            var result = await response.Content.ReadFromJsonAsync<HubKioskDeliveryResponse>(
                cancellationToken: cancellationToken);
            return result?.Delivered == true;
        }
        catch (Exception exception) when (!cancellationToken.IsCancellationRequested)
        {
            logger.LogWarning(exception, "Hub kiosk credential delivery failed");
            return false;
        }
    }

    private string BuildLoginUrl(string? dedicatedRealm)
    {
        var frontendUrl = configuration["Hub:FrontendUrl"]
            ?? configuration["FrontendUrl"]
            ?? "http://localhost:5173";
        var url = $"{frontendUrl.TrimEnd('/')}/kiosk";
        return dedicatedRealm is null
            ? $"{url}?realm="
            : $"{url}?realm={Uri.EscapeDataString(dedicatedRealm)}";
    }

    private static string BuildUsername(string slug, Guid tenantId)
    {
        var normalized = new string(slug
            .ToLowerInvariant()
            .Where(character => character is >= 'a' and <= 'z' or >= '0' and <= '9' or '-')
            .ToArray())
            .Trim('-');
        if (normalized.Length == 0)
            normalized = tenantId.ToString("N")[..8];
        if (normalized.Length > 48)
            normalized = normalized[..48].TrimEnd('-');
        return $"kiosk-{normalized}";
    }

    private static string GenerateTemporaryPassword()
    {
        var random = Convert.ToBase64String(RandomNumberGenerator.GetBytes(18))
            .Replace('+', '-')
            .Replace('/', '_')
            .TrimEnd('=');
        return $"Wb!{random}";
    }

    private sealed record HubKioskDeliveryResponse(bool Delivered);
}

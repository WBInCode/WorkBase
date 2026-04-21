using System.Net.Http.Json;
using System.Web;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using WorkBase.Modules.Integration.Application.Services;
using WorkBase.Modules.Integration.Domain.Enums;

namespace WorkBase.Modules.Integration.Infrastructure.Services;

internal sealed class OAuthFlowService(
    IHttpClientFactory httpClientFactory,
    IConfiguration configuration,
    ILogger<OAuthFlowService> logger) : IOAuthFlowService
{
    public string GetAuthorizationUrl(IntegrationProvider provider, Guid tenantId, Guid userId, string redirectUri, string? scopes = null)
    {
        return provider switch
        {
            IntegrationProvider.GoogleCalendar => BuildGoogleAuthUrl(redirectUri, scopes, tenantId, userId),
            IntegrationProvider.Microsoft365Calendar => BuildMicrosoftAuthUrl(redirectUri, scopes, tenantId, userId),
            IntegrationProvider.Slack => BuildSlackAuthUrl(redirectUri, scopes, tenantId, userId),
            IntegrationProvider.Gmail => BuildGoogleAuthUrl(redirectUri, scopes ?? "https://www.googleapis.com/auth/gmail.readonly", tenantId, userId),
            _ => throw new NotSupportedException($"OAuth not supported for provider: {provider}")
        };
    }

    public async Task<OAuthCallbackResult> ExchangeCodeAsync(IntegrationProvider provider, string code, string redirectUri, CancellationToken ct = default)
    {
        return provider switch
        {
            IntegrationProvider.GoogleCalendar or IntegrationProvider.Gmail => await ExchangeGoogleCodeAsync(code, redirectUri, ct),
            IntegrationProvider.Microsoft365Calendar => await ExchangeMicrosoftCodeAsync(code, redirectUri, ct),
            IntegrationProvider.Slack => await ExchangeSlackCodeAsync(code, redirectUri, ct),
            _ => throw new NotSupportedException($"OAuth code exchange not supported for provider: {provider}")
        };
    }

    public async Task<OAuthCallbackResult> RefreshTokenAsync(IntegrationProvider provider, string refreshToken, CancellationToken ct = default)
    {
        return provider switch
        {
            IntegrationProvider.GoogleCalendar or IntegrationProvider.Gmail => await RefreshGoogleTokenAsync(refreshToken, ct),
            IntegrationProvider.Microsoft365Calendar => await RefreshMicrosoftTokenAsync(refreshToken, ct),
            _ => throw new NotSupportedException($"Token refresh not supported for provider: {provider}")
        };
    }

    private string BuildGoogleAuthUrl(string redirectUri, string? scopes, Guid tenantId, Guid userId)
    {
        var clientId = configuration["Integration:Google:ClientId"] ?? string.Empty;
        scopes ??= "https://www.googleapis.com/auth/calendar";
        var state = $"{tenantId}:{userId}";

        return $"https://accounts.google.com/o/oauth2/v2/auth" +
               $"?client_id={HttpUtility.UrlEncode(clientId)}" +
               $"&redirect_uri={HttpUtility.UrlEncode(redirectUri)}" +
               $"&response_type=code" +
               $"&scope={HttpUtility.UrlEncode(scopes)}" +
               $"&access_type=offline" +
               $"&prompt=consent" +
               $"&state={HttpUtility.UrlEncode(state)}";
    }

    private string BuildMicrosoftAuthUrl(string redirectUri, string? scopes, Guid tenantId, Guid userId)
    {
        var clientId = configuration["Integration:Microsoft:ClientId"] ?? string.Empty;
        var tenantIdMs = configuration["Integration:Microsoft:TenantId"] ?? "common";
        scopes ??= "Calendars.ReadWrite offline_access";
        var state = $"{tenantId}:{userId}";

        return $"https://login.microsoftonline.com/{tenantIdMs}/oauth2/v2.0/authorize" +
               $"?client_id={HttpUtility.UrlEncode(clientId)}" +
               $"&redirect_uri={HttpUtility.UrlEncode(redirectUri)}" +
               $"&response_type=code" +
               $"&scope={HttpUtility.UrlEncode(scopes)}" +
               $"&state={HttpUtility.UrlEncode(state)}";
    }

    private string BuildSlackAuthUrl(string redirectUri, string? scopes, Guid tenantId, Guid userId)
    {
        var clientId = configuration["Integration:Slack:ClientId"] ?? string.Empty;
        scopes ??= "chat:write,channels:read";
        var state = $"{tenantId}:{userId}";

        return $"https://slack.com/oauth/v2/authorize" +
               $"?client_id={HttpUtility.UrlEncode(clientId)}" +
               $"&redirect_uri={HttpUtility.UrlEncode(redirectUri)}" +
               $"&scope={HttpUtility.UrlEncode(scopes)}" +
               $"&state={HttpUtility.UrlEncode(state)}";
    }

    private async Task<OAuthCallbackResult> ExchangeGoogleCodeAsync(string code, string redirectUri, CancellationToken ct)
    {
        var client = httpClientFactory.CreateClient("OAuth");
        var clientId = configuration["Integration:Google:ClientId"];
        var clientSecret = configuration["Integration:Google:ClientSecret"];

        var form = new FormUrlEncodedContent(new Dictionary<string, string?>
        {
            ["code"] = code,
            ["client_id"] = clientId,
            ["client_secret"] = clientSecret,
            ["redirect_uri"] = redirectUri,
            ["grant_type"] = "authorization_code"
        });

        var response = await client.PostAsync("https://oauth2.googleapis.com/token", form, ct);
        response.EnsureSuccessStatusCode();

        var tokenResponse = await response.Content.ReadFromJsonAsync<GoogleTokenResponse>(ct);
        logger.LogInformation("Exchanged Google OAuth code successfully");

        return new OAuthCallbackResult(
            tokenResponse!.AccessToken,
            tokenResponse.RefreshToken,
            DateTime.UtcNow.AddSeconds(tokenResponse.ExpiresIn),
            null, null);
    }

    private async Task<OAuthCallbackResult> ExchangeMicrosoftCodeAsync(string code, string redirectUri, CancellationToken ct)
    {
        var client = httpClientFactory.CreateClient("OAuth");
        var clientId = configuration["Integration:Microsoft:ClientId"];
        var clientSecret = configuration["Integration:Microsoft:ClientSecret"];
        var tenantId = configuration["Integration:Microsoft:TenantId"] ?? "common";

        var form = new FormUrlEncodedContent(new Dictionary<string, string?>
        {
            ["code"] = code,
            ["client_id"] = clientId,
            ["client_secret"] = clientSecret,
            ["redirect_uri"] = redirectUri,
            ["grant_type"] = "authorization_code",
            ["scope"] = "Calendars.ReadWrite offline_access"
        });

        var response = await client.PostAsync($"https://login.microsoftonline.com/{tenantId}/oauth2/v2.0/token", form, ct);
        response.EnsureSuccessStatusCode();

        var tokenResponse = await response.Content.ReadFromJsonAsync<MicrosoftTokenResponse>(ct);
        logger.LogInformation("Exchanged Microsoft OAuth code successfully");

        return new OAuthCallbackResult(
            tokenResponse!.AccessToken,
            tokenResponse.RefreshToken,
            DateTime.UtcNow.AddSeconds(tokenResponse.ExpiresIn),
            null, null);
    }

    private async Task<OAuthCallbackResult> ExchangeSlackCodeAsync(string code, string redirectUri, CancellationToken ct)
    {
        var client = httpClientFactory.CreateClient("OAuth");
        var clientId = configuration["Integration:Slack:ClientId"];
        var clientSecret = configuration["Integration:Slack:ClientSecret"];

        var form = new FormUrlEncodedContent(new Dictionary<string, string?>
        {
            ["code"] = code,
            ["client_id"] = clientId,
            ["client_secret"] = clientSecret,
            ["redirect_uri"] = redirectUri
        });

        var response = await client.PostAsync("https://slack.com/api/oauth.v2.access", form, ct);
        response.EnsureSuccessStatusCode();

        var tokenResponse = await response.Content.ReadFromJsonAsync<SlackTokenResponse>(ct);
        logger.LogInformation("Exchanged Slack OAuth code successfully");

        return new OAuthCallbackResult(
            tokenResponse!.AccessToken ?? string.Empty,
            null,
            DateTime.UtcNow.AddYears(1), // Slack tokens don't expire
            tokenResponse.Team?.Id,
            tokenResponse.Team?.Name);
    }

    private async Task<OAuthCallbackResult> RefreshGoogleTokenAsync(string refreshToken, CancellationToken ct)
    {
        var client = httpClientFactory.CreateClient("OAuth");
        var clientId = configuration["Integration:Google:ClientId"];
        var clientSecret = configuration["Integration:Google:ClientSecret"];

        var form = new FormUrlEncodedContent(new Dictionary<string, string?>
        {
            ["refresh_token"] = refreshToken,
            ["client_id"] = clientId,
            ["client_secret"] = clientSecret,
            ["grant_type"] = "refresh_token"
        });

        var response = await client.PostAsync("https://oauth2.googleapis.com/token", form, ct);
        response.EnsureSuccessStatusCode();

        var tokenResponse = await response.Content.ReadFromJsonAsync<GoogleTokenResponse>(ct);
        return new OAuthCallbackResult(
            tokenResponse!.AccessToken,
            tokenResponse.RefreshToken ?? refreshToken,
            DateTime.UtcNow.AddSeconds(tokenResponse.ExpiresIn),
            null, null);
    }

    private async Task<OAuthCallbackResult> RefreshMicrosoftTokenAsync(string refreshToken, CancellationToken ct)
    {
        var client = httpClientFactory.CreateClient("OAuth");
        var clientId = configuration["Integration:Microsoft:ClientId"];
        var clientSecret = configuration["Integration:Microsoft:ClientSecret"];
        var tenantId = configuration["Integration:Microsoft:TenantId"] ?? "common";

        var form = new FormUrlEncodedContent(new Dictionary<string, string?>
        {
            ["refresh_token"] = refreshToken,
            ["client_id"] = clientId,
            ["client_secret"] = clientSecret,
            ["grant_type"] = "refresh_token",
            ["scope"] = "Calendars.ReadWrite offline_access"
        });

        var response = await client.PostAsync($"https://login.microsoftonline.com/{tenantId}/oauth2/v2.0/token", form, ct);
        response.EnsureSuccessStatusCode();

        var tokenResponse = await response.Content.ReadFromJsonAsync<MicrosoftTokenResponse>(ct);
        return new OAuthCallbackResult(
            tokenResponse!.AccessToken,
            tokenResponse.RefreshToken ?? refreshToken,
            DateTime.UtcNow.AddSeconds(tokenResponse.ExpiresIn),
            null, null);
    }

    // Token response DTOs
    private sealed record GoogleTokenResponse(
        string AccessToken,
        string? RefreshToken,
        int ExpiresIn);

    private sealed record MicrosoftTokenResponse(
        string AccessToken,
        string? RefreshToken,
        int ExpiresIn);

    private sealed record SlackTokenResponse(
        string? AccessToken,
        SlackTeam? Team);

    private sealed record SlackTeam(string? Id, string? Name);
}

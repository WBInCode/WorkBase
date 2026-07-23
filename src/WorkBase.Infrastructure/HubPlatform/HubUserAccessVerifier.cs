using System.Net.Http.Json;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace WorkBase.Infrastructure.HubPlatform;

public sealed record HubUserAccessDecision(bool Active, string? HubRole);

public sealed class HubUserAccessVerifier(
    IHttpClientFactory httpClientFactory,
    IConfiguration configuration,
    IMemoryCache cache,
    ILogger<HubUserAccessVerifier> logger)
{
    private sealed record HubAccessResponse(
        bool Active,
        string? HubUserId,
        string? OrgRole,
        string? InstanceRole,
        string? Reason);

    private static readonly TimeSpan PositiveLifetime = TimeSpan.FromSeconds(30);
    private static readonly TimeSpan NegativeLifetime = TimeSpan.FromSeconds(10);

    public async Task<HubUserAccessDecision> VerifyAsync(
        string instanceId,
        string hubUserId,
        string email,
        CancellationToken cancellationToken = default)
    {
        var normalizedEmail = email.Trim().ToLowerInvariant();
        var cacheKey = $"hub-user-access:{instanceId}:{hubUserId}";
        if (cache.TryGetValue<HubUserAccessDecision>(cacheKey, out var cached) && cached is not null)
            return cached;

        var options = configuration.GetSection(HubOptions.SectionName).Get<HubOptions>() ?? new HubOptions();
        if (!options.Enabled
            || string.IsNullOrWhiteSpace(options.BaseUrl)
            || string.IsNullOrWhiteSpace(options.ClientSecret))
        {
            return new HubUserAccessDecision(false, null);
        }

        try
        {
            var escapedInstanceId = Uri.EscapeDataString(instanceId);
            using var request = new HttpRequestMessage(
                HttpMethod.Post,
                $"{options.BaseUrl.TrimEnd('/')}/api/v1/instances/{escapedInstanceId}/user-access/check");
            request.Headers.Add("x-sso-client-id", options.ClientId);
            request.Headers.Add("x-sso-secret", options.ClientSecret);
            request.Content = JsonContent.Create(new { hubUserId, email = normalizedEmail });

            using var response = await httpClientFactory.CreateClient("hub-platform")
                .SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning(
                    "HUB user access check failed for instance {InstanceId}: HTTP {StatusCode}",
                    instanceId, (int)response.StatusCode);
                return new HubUserAccessDecision(false, null);
            }

            var result = await response.Content.ReadFromJsonAsync<HubAccessResponse>(cancellationToken: cancellationToken);
            var decision = result?.Active == true
                ? new HubUserAccessDecision(
                    true,
                    HubSsoService.MapHubRole(result.OrgRole ?? "", result.InstanceRole ?? ""))
                : new HubUserAccessDecision(false, null);
            cache.Set(cacheKey, decision, decision.Active ? PositiveLifetime : NegativeLifetime);
            return decision;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogWarning(ex, "HUB user access check failed for instance {InstanceId}", instanceId);
            return new HubUserAccessDecision(false, null);
        }
    }
}
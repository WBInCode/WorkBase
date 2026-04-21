using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace WorkBase.Infrastructure.PublicApi;

public interface IApiKeyService
{
    (string rawKey, string hash, string prefix) GenerateKey();
    string HashKey(string rawKey);
    bool ValidateKey(string rawKey, string storedHash);
}

public sealed class ApiKeyService : IApiKeyService
{
    public (string rawKey, string hash, string prefix) GenerateKey()
    {
        var bytes = RandomNumberGenerator.GetBytes(32);
        var rawKey = $"wb_{Convert.ToBase64String(bytes).Replace("+", "").Replace("/", "").Replace("=", "")}";
        var hash = HashKey(rawKey);
        var prefix = rawKey[..11]; // "wb_" + 8 chars
        return (rawKey, hash, prefix);
    }

    public string HashKey(string rawKey)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(rawKey));
        return Convert.ToBase64String(bytes);
    }

    public bool ValidateKey(string rawKey, string storedHash)
    {
        return HashKey(rawKey) == storedHash;
    }
}

public interface IWebhookDispatcher
{
    Task DispatchAsync(Guid tenantId, string eventType, object payload, CancellationToken ct = default);
}

public sealed class WebhookDispatcher(
    IHttpClientFactory httpClientFactory,
    IWebhookSubscriptionRepository subscriptionRepo,
    IWebhookDeliveryLogRepository logRepo,
    ILogger<WebhookDispatcher> logger) : IWebhookDispatcher
{
    public async Task DispatchAsync(Guid tenantId, string eventType, object payload, CancellationToken ct = default)
    {
        var subscriptions = await subscriptionRepo.GetActiveByTenantAsync(tenantId, ct);
        var json = JsonSerializer.Serialize(payload);

        foreach (var sub in subscriptions)
        {
            var events = JsonSerializer.Deserialize<List<string>>(sub.EventsJson) ?? [];
            if (!events.Contains(eventType) && !events.Contains("*")) continue;

            await DeliverAsync(tenantId, sub, eventType, json, ct);
        }
    }

    private async Task DeliverAsync(Guid tenantId, WebhookSubscription sub,
        string eventType, string payloadJson, CancellationToken ct)
    {
        var client = httpClientFactory.CreateClient("Webhook");

        for (var attempt = 1; attempt <= sub.MaxRetries; attempt++)
        {
            try
            {
                using var request = new HttpRequestMessage(HttpMethod.Post, sub.Url);
                request.Content = new StringContent(payloadJson, Encoding.UTF8, "application/json");
                request.Headers.Add("X-Webhook-Event", eventType);
                request.Headers.Add("X-Webhook-Attempt", attempt.ToString());

                if (!string.IsNullOrEmpty(sub.Secret))
                {
                    var signature = ComputeHmac(payloadJson, sub.Secret);
                    request.Headers.Add("X-Webhook-Signature", $"sha256={signature}");
                }

                var response = await client.SendAsync(request, ct);
                var responseBody = await response.Content.ReadAsStringAsync(ct);
                var isSuccess = response.IsSuccessStatusCode;

                var log = WebhookDeliveryLog.Create(tenantId, sub.Id, eventType, payloadJson,
                    sub.Url, (int)response.StatusCode, responseBody, attempt, isSuccess);
                await logRepo.AddAsync(log, ct);

                sub.RecordDelivery(isSuccess ? "Success" : $"Failed:{(int)response.StatusCode}");

                if (isSuccess)
                {
                    logger.LogInformation("Webhook delivered: {Event} to {Url}", eventType, sub.Url);
                    return;
                }
            }
            catch (Exception ex)
            {
                var log = WebhookDeliveryLog.Create(tenantId, sub.Id, eventType, payloadJson,
                    sub.Url, 0, null, attempt, false, ex.Message);
                await logRepo.AddAsync(log, ct);
                sub.RecordDelivery($"Error:{ex.Message[..Math.Min(100, ex.Message.Length)]}");

                logger.LogWarning(ex, "Webhook delivery failed: {Event} to {Url}, attempt {Attempt}",
                    eventType, sub.Url, attempt);
            }
        }
    }

    private static string ComputeHmac(string payload, string secret)
    {
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(payload));
        return Convert.ToHexStringLower(hash);
    }
}

public interface IApiKeyRepository
{
    Task<ApiKey?> GetByHashAsync(string keyHash, CancellationToken ct = default);
    Task<ApiKey?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<List<ApiKey>> GetByTenantAsync(Guid tenantId, CancellationToken ct = default);
    Task AddAsync(ApiKey key, CancellationToken ct = default);
    void Update(ApiKey key);
    void Remove(ApiKey key);
}

public interface IWebhookSubscriptionRepository
{
    Task<WebhookSubscription?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<List<WebhookSubscription>> GetByTenantAsync(Guid tenantId, CancellationToken ct = default);
    Task<List<WebhookSubscription>> GetActiveByTenantAsync(Guid tenantId, CancellationToken ct = default);
    Task AddAsync(WebhookSubscription sub, CancellationToken ct = default);
    void Update(WebhookSubscription sub);
    void Remove(WebhookSubscription sub);
}

public interface IWebhookDeliveryLogRepository
{
    Task<List<WebhookDeliveryLog>> GetBySubscriptionAsync(Guid subscriptionId, int limit = 50, CancellationToken ct = default);
    Task AddAsync(WebhookDeliveryLog log, CancellationToken ct = default);
}

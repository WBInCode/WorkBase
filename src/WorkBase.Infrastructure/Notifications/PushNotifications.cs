using System.Net.Http.Json;
using Microsoft.Extensions.Logging;
using WorkBase.Shared.Domain;

namespace WorkBase.Infrastructure.Notifications;

public sealed class PushSubscription : Entity<Guid>, ITenantScoped
{
    public Guid TenantId { get; private set; }
    public Guid UserId { get; private set; }
    public string DeviceToken { get; private set; } = null!; // FCM token
    public string Platform { get; private set; } = null!; // "web", "android", "ios"
    public string? DeviceName { get; private set; }
    public bool IsActive { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? LastUsedAt { get; private set; }

    private PushSubscription() { }

    public static PushSubscription Create(
        Guid tenantId, Guid userId, string deviceToken, string platform, string? deviceName = null)
    {
        return new PushSubscription
        {
            TenantId = tenantId,
            UserId = userId,
            DeviceToken = deviceToken,
            Platform = platform,
            DeviceName = deviceName,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
        };
    }

    public void UpdateToken(string newToken) { DeviceToken = newToken; LastUsedAt = DateTime.UtcNow; }
    public void Deactivate() => IsActive = false;
}

public interface IPushSubscriptionRepository
{
    Task<List<PushSubscription>> GetByUserAsync(Guid userId, CancellationToken ct = default);
    Task<List<PushSubscription>> GetActiveByUserAsync(Guid userId, CancellationToken ct = default);
    Task<PushSubscription?> GetByTokenAsync(string deviceToken, CancellationToken ct = default);
    Task AddAsync(PushSubscription sub, CancellationToken ct = default);
    void Update(PushSubscription sub);
    void Remove(PushSubscription sub);
}

public interface IPushNotificationService
{
    Task SendToUserAsync(Guid userId, string title, string body, Dictionary<string, string>? data = null, CancellationToken ct = default);
    Task SendToUsersAsync(IEnumerable<Guid> userIds, string title, string body, Dictionary<string, string>? data = null, CancellationToken ct = default);
}

public sealed class FcmPushNotificationService(
    IPushSubscriptionRepository subscriptionRepo,
    IHttpClientFactory httpClientFactory,
    Microsoft.Extensions.Logging.ILogger<FcmPushNotificationService> logger) : IPushNotificationService
{
    public async Task SendToUserAsync(Guid userId, string title, string body, Dictionary<string, string>? data = null, CancellationToken ct = default)
    {
        var subs = await subscriptionRepo.GetActiveByUserAsync(userId, ct);
        foreach (var sub in subs)
        {
            await SendToDeviceAsync(sub.DeviceToken, title, body, data, ct);
        }
    }

    public async Task SendToUsersAsync(IEnumerable<Guid> userIds, string title, string body, Dictionary<string, string>? data = null, CancellationToken ct = default)
    {
        foreach (var userId in userIds)
            await SendToUserAsync(userId, title, body, data, ct);
    }

    private async Task SendToDeviceAsync(string token, string title, string body, Dictionary<string, string>? data, CancellationToken ct)
    {
        try
        {
            var client = httpClientFactory.CreateClient("FCM");
            var payload = new
            {
                message = new
                {
                    token,
                    notification = new { title, body },
                    data,
                },
            };

            var response = await client.PostAsJsonAsync("https://fcm.googleapis.com/v1/projects/-/messages:send", payload, ct);
            if (!response.IsSuccessStatusCode)
            {
                var err = await response.Content.ReadAsStringAsync(ct);
                logger.LogWarning("FCM send failed for token {Token}: {Error}", token[..8], err);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "FCM send error for token {Token}", token[..8]);
        }
    }
}

/// <summary>In-memory stub until EF persistence is wired.</summary>
public sealed class InMemoryPushSubscriptionRepository : IPushSubscriptionRepository
{
    public Task<List<PushSubscription>> GetByUserAsync(Guid userId, CancellationToken ct = default) => Task.FromResult(new List<PushSubscription>());
    public Task<List<PushSubscription>> GetActiveByUserAsync(Guid userId, CancellationToken ct = default) => Task.FromResult(new List<PushSubscription>());
    public Task<PushSubscription?> GetByTokenAsync(string deviceToken, CancellationToken ct = default) => Task.FromResult<PushSubscription?>(null);
    public Task AddAsync(PushSubscription sub, CancellationToken ct = default) => Task.CompletedTask;
    public void Update(PushSubscription sub) { }
    public void Remove(PushSubscription sub) { }
}

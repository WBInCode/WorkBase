using WorkBase.Shared.Domain;

namespace WorkBase.Modules.Notification.Domain.Entities;

public sealed class PushSubscription : Entity<Guid>, ITenantScoped
{
    public Guid TenantId { get; private set; }
    public Guid UserId { get; private set; }
    public string Endpoint { get; private set; } = null!;
    public string P256dh { get; private set; } = null!;
    public string Auth { get; private set; } = null!;
    public string? DeviceInfo { get; private set; }
    public DateTime CreatedAt { get; private set; }

    private PushSubscription() { }

    public static PushSubscription Create(
        Guid tenantId, Guid userId,
        string endpoint, string p256dh, string auth,
        string? deviceInfo = null)
    {
        return new PushSubscription
        {
            TenantId = tenantId,
            UserId = userId,
            Endpoint = endpoint,
            P256dh = p256dh,
            Auth = auth,
            DeviceInfo = deviceInfo,
            CreatedAt = DateTime.UtcNow,
        };
    }
}

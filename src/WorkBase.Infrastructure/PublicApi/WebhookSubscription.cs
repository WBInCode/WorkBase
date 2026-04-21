using WorkBase.Shared.Domain;

namespace WorkBase.Infrastructure.PublicApi;

public sealed class WebhookSubscription : AuditableEntity<Guid>, ITenantScoped, IAuditable
{
    public Guid TenantId { get; private set; }
    public string Name { get; private set; } = null!;
    public string Url { get; private set; } = null!;
    public string? Secret { get; private set; } // HMAC-SHA256 signing secret
    public string EventsJson { get; private set; } = null!; // JSON: ["employee.created","leave.submitted"]
    public bool IsActive { get; private set; }
    public int MaxRetries { get; private set; }
    public DateTime? LastDeliveryAt { get; private set; }
    public string? LastDeliveryStatus { get; private set; }

    private WebhookSubscription() { }

    public static WebhookSubscription Create(
        Guid tenantId, string name, string url, string eventsJson,
        string? secret = null, int maxRetries = 3)
    {
        return new WebhookSubscription
        {
            TenantId = tenantId,
            Name = name,
            Url = url,
            Secret = secret,
            EventsJson = eventsJson,
            IsActive = true,
            MaxRetries = maxRetries,
        };
    }

    public void Update(string name, string url, string eventsJson, string? secret, int maxRetries)
    {
        Name = name; Url = url; EventsJson = eventsJson; Secret = secret; MaxRetries = maxRetries;
    }

    public void Activate() => IsActive = true;
    public void Deactivate() => IsActive = false;

    public void RecordDelivery(string status)
    {
        LastDeliveryAt = DateTime.UtcNow;
        LastDeliveryStatus = status;
    }
}

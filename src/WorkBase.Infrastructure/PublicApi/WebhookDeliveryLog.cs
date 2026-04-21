using WorkBase.Shared.Domain;

namespace WorkBase.Infrastructure.PublicApi;

public sealed class WebhookDeliveryLog : Entity<Guid>, ITenantScoped
{
    public Guid TenantId { get; private set; }
    public Guid SubscriptionId { get; private set; }
    public string EventType { get; private set; } = null!;
    public string PayloadJson { get; private set; } = null!;
    public string Url { get; private set; } = null!;
    public int StatusCode { get; private set; }
    public string? ResponseBody { get; private set; }
    public int AttemptNumber { get; private set; }
    public bool IsSuccess { get; private set; }
    public DateTime DeliveredAt { get; private set; }
    public string? ErrorMessage { get; private set; }

    private WebhookDeliveryLog() { }

    public static WebhookDeliveryLog Create(
        Guid tenantId, Guid subscriptionId, string eventType,
        string payloadJson, string url, int statusCode,
        string? responseBody, int attemptNumber, bool isSuccess,
        string? errorMessage = null)
    {
        return new WebhookDeliveryLog
        {
            TenantId = tenantId,
            SubscriptionId = subscriptionId,
            EventType = eventType,
            PayloadJson = payloadJson,
            Url = url,
            StatusCode = statusCode,
            ResponseBody = responseBody?.Length > 4000 ? responseBody[..4000] : responseBody,
            AttemptNumber = attemptNumber,
            IsSuccess = isSuccess,
            DeliveredAt = DateTime.UtcNow,
            ErrorMessage = errorMessage,
        };
    }
}

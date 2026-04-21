using WorkBase.Modules.Integration.Domain.Enums;
using WorkBase.Shared.Domain;

namespace WorkBase.Modules.Integration.Domain.Entities;

public sealed class WebhookRegistration : AuditableEntity<Guid>, ITenantScoped
{
    public Guid TenantId { get; private set; }
    public IntegrationProvider Provider { get; private set; }
    public string WebhookUrl { get; private set; } = string.Empty;
    public string? Secret { get; private set; }
    public string EventTypes { get; private set; } = string.Empty;
    public bool IsActive { get; private set; }

    private WebhookRegistration() { }

    public static WebhookRegistration Create(
        Guid tenantId, IntegrationProvider provider,
        string webhookUrl, string? secret, string eventTypes)
    {
        return new WebhookRegistration
        {
            TenantId = tenantId,
            Provider = provider,
            WebhookUrl = webhookUrl,
            Secret = secret,
            EventTypes = eventTypes,
            IsActive = true
        };
    }

    public void Deactivate() => IsActive = false;
    public void Activate() => IsActive = true;
}

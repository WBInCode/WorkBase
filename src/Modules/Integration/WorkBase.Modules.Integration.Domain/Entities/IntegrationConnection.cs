using WorkBase.Modules.Integration.Domain.Enums;
using WorkBase.Shared.Domain;

namespace WorkBase.Modules.Integration.Domain.Entities;

public sealed class IntegrationConnection : AuditableEntity<Guid>, ITenantScoped
{
    public Guid TenantId { get; private set; }
    public Guid UserId { get; private set; }
    public IntegrationProvider Provider { get; private set; }
    public string ExternalAccountId { get; private set; } = string.Empty;
    public string DisplayName { get; private set; } = string.Empty;
    public bool IsActive { get; private set; }

    private IntegrationConnection() { }

    public static IntegrationConnection Create(
        Guid tenantId, Guid userId, IntegrationProvider provider,
        string externalAccountId, string displayName)
    {
        return new IntegrationConnection
        {
            TenantId = tenantId,
            UserId = userId,
            Provider = provider,
            ExternalAccountId = externalAccountId,
            DisplayName = displayName,
            IsActive = true
        };
    }

    public void Deactivate() => IsActive = false;
    public void Activate() => IsActive = true;
}

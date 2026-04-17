using WorkBase.Shared.Domain;

namespace WorkBase.Modules.Notification.Domain.Entities;

public sealed class NotificationPreference : Entity<Guid>, ITenantScoped
{
    public Guid TenantId { get; private set; }
    public Guid UserId { get; private set; }
    public string Category { get; private set; } = null!;
    public bool InApp { get; private set; }
    public bool Email { get; private set; }

    private NotificationPreference() { }

    public static NotificationPreference Create(
        Guid tenantId, Guid userId, string category,
        bool inApp = true, bool email = false)
    {
        return new NotificationPreference
        {
            TenantId = tenantId,
            UserId = userId,
            Category = category,
            InApp = inApp,
            Email = email
        };
    }

    public void Update(bool inApp, bool email)
    {
        InApp = inApp;
        Email = email;
    }
}

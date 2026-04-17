using WorkBase.Shared.Domain;

namespace WorkBase.Modules.Notification.Domain.Entities;

public sealed class Notification : AuditableEntity<Guid>, ITenantScoped, IAuditable
{
    public Guid TenantId { get; private set; }
    public Guid RecipientUserId { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public string Body { get; private set; } = string.Empty;
    public string Category { get; private set; } = string.Empty;
    public bool IsRead { get; private set; }
    public DateTime? ReadAt { get; private set; }
    public string? ReferenceType { get; private set; }
    public Guid? ReferenceId { get; private set; }

    private Notification() { }

    public static Notification Create(
        Guid tenantId,
        Guid recipientUserId,
        string title,
        string body,
        string category,
        string? referenceType = null,
        Guid? referenceId = null)
    {
        return new Notification
        {
            TenantId = tenantId,
            RecipientUserId = recipientUserId,
            Title = title,
            Body = body,
            Category = category,
            ReferenceType = referenceType,
            ReferenceId = referenceId
        };
    }

    public void MarkAsRead()
    {
        if (IsRead) return;
        IsRead = true;
        ReadAt = DateTime.UtcNow;
    }
}

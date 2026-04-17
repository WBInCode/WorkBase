using WorkBase.Shared.Domain;

namespace WorkBase.Modules.Notification.Domain.Entities;

public sealed class NotificationTemplate : AuditableEntity<Guid>, ITenantScoped, IAuditable
{
    public Guid TenantId { get; private set; }
    public string Code { get; private set; } = null!;
    public string Name { get; private set; } = null!;
    public string TitleTemplate { get; private set; } = null!;
    public string BodyTemplate { get; private set; } = null!;
    public string Category { get; private set; } = null!;
    public bool IsActive { get; private set; }

    private NotificationTemplate() { }

    public static NotificationTemplate Create(
        Guid tenantId, string code, string name,
        string titleTemplate, string bodyTemplate,
        string category)
    {
        return new NotificationTemplate
        {
            TenantId = tenantId,
            Code = code,
            Name = name,
            TitleTemplate = titleTemplate,
            BodyTemplate = bodyTemplate,
            Category = category,
            IsActive = true
        };
    }

    public void Update(string name, string titleTemplate, string bodyTemplate, string category)
    {
        Name = name;
        TitleTemplate = titleTemplate;
        BodyTemplate = bodyTemplate;
        Category = category;
    }

    public void Activate() => IsActive = true;
    public void Deactivate() => IsActive = false;
}

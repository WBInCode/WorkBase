using WorkBase.Shared.Domain;

namespace WorkBase.Modules.Documents.Domain.Entities;

public sealed class DocumentCategory : AuditableEntity<Guid>, ITenantScoped, IAuditable
{
    public Guid TenantId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string? Description { get; private set; }

    private DocumentCategory() { }

    public static DocumentCategory Create(Guid tenantId, string name, string? description = null)
    {
        return new DocumentCategory
        {
            TenantId = tenantId,
            Name = name,
            Description = description
        };
    }

    public void Update(string name, string? description)
    {
        Name = name;
        Description = description;
    }
}

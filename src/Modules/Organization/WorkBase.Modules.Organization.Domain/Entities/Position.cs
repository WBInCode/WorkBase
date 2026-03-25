using WorkBase.Shared.Domain;

namespace WorkBase.Modules.Organization.Domain.Entities;

public sealed class Position : AuditableEntity<Guid>, ITenantScoped, IAuditable
{
    public Guid TenantId { get; private set; }
    public string Name { get; private set; } = null!;
    public string? Description { get; private set; }
    public bool IsActive { get; private set; }

    private Position() { }

    public static Position Create(Guid tenantId, string name, string? description)
    {
        return new Position
        {
            TenantId = tenantId,
            Name = name,
            Description = description,
            IsActive = true
        };
    }

    public void Update(string name, string? description)
    {
        Name = name;
        Description = description;
    }

    public void Deactivate() => IsActive = false;

    public void Activate() => IsActive = true;
}

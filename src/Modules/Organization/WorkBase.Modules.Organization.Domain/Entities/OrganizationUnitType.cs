using WorkBase.Shared.Domain;

namespace WorkBase.Modules.Organization.Domain.Entities;

public sealed class OrganizationUnitType : AuditableEntity<Guid>, ITenantScoped, IAuditable
{
    public Guid TenantId { get; private set; }
    public string Name { get; private set; } = null!;
    public string? Description { get; private set; }
    public int SortOrder { get; private set; }
    public bool IsActive { get; private set; }

    private OrganizationUnitType() { }

    public static OrganizationUnitType Create(
        Guid tenantId,
        string name,
        string? description,
        int sortOrder)
    {
        return new OrganizationUnitType
        {
            TenantId = tenantId,
            Name = name,
            Description = description,
            SortOrder = sortOrder,
            IsActive = true
        };
    }

    public void Update(string name, string? description, int sortOrder)
    {
        Name = name;
        Description = description;
        SortOrder = sortOrder;
    }

    public void Deactivate() => IsActive = false;

    public void Activate() => IsActive = true;
}

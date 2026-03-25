using WorkBase.Shared.Domain;

namespace WorkBase.Modules.Organization.Domain.Entities;

public sealed class OrganizationUnit : AuditableEntity<Guid>, ITenantScoped, IAuditable
{
    public Guid TenantId { get; private set; }
    public string Name { get; private set; } = null!;
    public string? Code { get; private set; }
    public Guid TypeId { get; private set; }
    public Guid? ParentId { get; private set; }
    public bool IsActive { get; private set; }

    private OrganizationUnit() { }

    public static OrganizationUnit Create(
        Guid tenantId,
        string name,
        string? code,
        Guid typeId,
        Guid? parentId)
    {
        return new OrganizationUnit
        {
            TenantId = tenantId,
            Name = name,
            Code = code,
            TypeId = typeId,
            ParentId = parentId,
            IsActive = true
        };
    }

    public void Update(string name, string? code, Guid typeId)
    {
        Name = name;
        Code = code;
        TypeId = typeId;
    }

    public void Move(Guid? newParentId)
    {
        ParentId = newParentId;
    }

    public void Deactivate() => IsActive = false;

    public void Activate() => IsActive = true;
}

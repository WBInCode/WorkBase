using WorkBase.Shared.Domain;

namespace WorkBase.Modules.Organization.Domain.Entities;

public sealed class Position : AuditableEntity<Guid>, ITenantScoped, IAuditable
{
    public Guid TenantId { get; private set; }
    public string Name { get; private set; } = null!;
    public string? Description { get; private set; }
    public bool IsActive { get; private set; }

    /// <summary>Rola WorkBase nadawana pracownikowi przy przypisaniu na to stanowisko.</summary>
    public Guid? DefaultRoleId { get; private set; }

    /// <summary>Stanowisko kierownicze — obejmuje przelozenstwo nad jednostką, do której trafia pracownik.</summary>
    public bool IsManagerial { get; private set; }

    private Position() { }

    public static Position Create(Guid tenantId, string name, string? description, Guid? defaultRoleId = null, bool isManagerial = false)
    {
        return new Position
        {
            TenantId = tenantId,
            Name = name,
            Description = description,
            DefaultRoleId = defaultRoleId,
            IsManagerial = isManagerial,
            IsActive = true
        };
    }

    public void Update(string name, string? description, Guid? defaultRoleId = null, bool isManagerial = false)
    {
        Name = name;
        Description = description;
        DefaultRoleId = defaultRoleId;
        IsManagerial = isManagerial;
    }

    public void Deactivate() => IsActive = false;

    public void Activate() => IsActive = true;
}

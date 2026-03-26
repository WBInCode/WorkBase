using WorkBase.Shared.Domain;

namespace WorkBase.Modules.Identity.Domain.Entities;

public sealed class Role : AuditableEntity<Guid>, ITenantScoped
{
    public Guid TenantId { get; private set; }
    public string Name { get; private set; } = null!;
    public string? Description { get; private set; }
    public RoleType Type { get; private set; }
    public bool IsActive { get; private set; }
    public int Level { get; private set; }

    private readonly List<RolePermission> _rolePermissions = [];
    public IReadOnlyCollection<RolePermission> RolePermissions => _rolePermissions.AsReadOnly();

    private readonly List<UserRole> _userRoles = [];
    public IReadOnlyCollection<UserRole> UserRoles => _userRoles.AsReadOnly();

    private Role() { }

    public static Role Create(
        Guid tenantId,
        string name,
        RoleType type,
        int level = 0,
        string? description = null)
    {
        return new Role
        {
            TenantId = tenantId,
            Name = name,
            Type = type,
            Level = level,
            Description = description,
            IsActive = true
        };
    }

    public void Update(string name, string? description, int level)
    {
        Name = name;
        Description = description;
        Level = level;
    }

    public void Deactivate() => IsActive = false;
    public void Activate() => IsActive = true;

    public bool IsSystemRole => Type == RoleType.System;
}

public enum RoleType
{
    System,
    Organizational,
    Custom
}

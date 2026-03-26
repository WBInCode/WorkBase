using WorkBase.Shared.Domain;

namespace WorkBase.Modules.Identity.Domain.Entities;

public sealed class Permission : Entity<Guid>
{
    public string Module { get; private set; } = null!;
    public string Action { get; private set; } = null!;
    public string? Scope { get; private set; }
    public string? Description { get; private set; }

    private readonly List<RolePermission> _rolePermissions = [];
    public IReadOnlyCollection<RolePermission> RolePermissions => _rolePermissions.AsReadOnly();

    public string FullCode => Scope is not null
        ? $"{Module}.{Action}.{Scope}"
        : $"{Module}.{Action}";

    private Permission() { }

    public static Permission Create(
        string module,
        string action,
        string? scope = null,
        string? description = null)
    {
        return new Permission
        {
            Module = module,
            Action = action,
            Scope = scope,
            Description = description
        };
    }
}

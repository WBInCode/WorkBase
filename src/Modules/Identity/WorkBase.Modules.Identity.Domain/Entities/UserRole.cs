using WorkBase.Shared.Domain;

namespace WorkBase.Modules.Identity.Domain.Entities;

public sealed class UserRole : Entity<Guid>, ITenantScoped
{
    public Guid UserId { get; private set; }
    public User User { get; private set; } = null!;

    public Guid RoleId { get; private set; }
    public Role Role { get; private set; } = null!;

    public Guid TenantId { get; private set; }
    public DateTime AssignedAt { get; private set; }
    public string? AssignedBy { get; private set; }

    private UserRole() { }

    public static UserRole Create(Guid userId, Guid roleId, Guid tenantId, string? assignedBy = null)
    {
        return new UserRole
        {
            UserId = userId,
            RoleId = roleId,
            TenantId = tenantId,
            AssignedAt = DateTime.UtcNow,
            AssignedBy = assignedBy
        };
    }
}

using WorkBase.Shared.Domain;

namespace WorkBase.Modules.Dashboard.Domain.Entities;

public sealed class DashboardConfig : Entity<Guid>, ITenantScoped
{
    public Guid TenantId { get; private set; }
    public Guid UserId { get; private set; }
    public string Name { get; private set; } = null!;
    public bool IsDefault { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? ModifiedAt { get; private set; }

    private DashboardConfig() { }

    public static DashboardConfig Create(
        Guid tenantId, Guid userId, string name, bool isDefault = false)
    {
        return new DashboardConfig
        {
            TenantId = tenantId,
            UserId = userId,
            Name = name,
            IsDefault = isDefault,
            CreatedAt = DateTime.UtcNow,
        };
    }

    public void Update(string name, bool isDefault)
    {
        Name = name;
        IsDefault = isDefault;
        ModifiedAt = DateTime.UtcNow;
    }
}

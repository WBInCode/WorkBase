using WorkBase.Shared.Domain;

namespace WorkBase.Modules.Identity.Domain.Entities;

public sealed class FeatureFlag : Entity<Guid>, ITenantScoped
{
    public Guid TenantId { get; private set; }
    public string Module { get; private set; } = null!;
    public bool IsEnabled { get; private set; }
    public DateTime? EnabledAt { get; private set; }
    public string? EnabledBy { get; private set; }

    private FeatureFlag() { }

    public static FeatureFlag Create(Guid tenantId, string module, bool isEnabled = true, string? enabledBy = null)
    {
        return new FeatureFlag
        {
            TenantId = tenantId,
            Module = module,
            IsEnabled = isEnabled,
            EnabledAt = isEnabled ? DateTime.UtcNow : null,
            EnabledBy = enabledBy
        };
    }

    public void Enable(string? enabledBy = null)
    {
        IsEnabled = true;
        EnabledAt = DateTime.UtcNow;
        EnabledBy = enabledBy;
    }

    public void Disable()
    {
        IsEnabled = false;
        EnabledAt = null;
        EnabledBy = null;
    }
}

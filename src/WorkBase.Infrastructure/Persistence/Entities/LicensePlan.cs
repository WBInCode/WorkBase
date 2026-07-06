namespace WorkBase.Infrastructure.Persistence.Entities;

/// <summary>
/// A named bundle of modules (e.g. "Bronze"/"Silver"/"Gold") that can be applied to a
/// Tenant as a quick starting point. The authoritative, real-time state of which modules
/// are enabled always lives in FeatureFlag rows — applying a plan simply (re)writes those
/// rows to match <see cref="IncludedModules"/>. See docs/05-module-licensing-architecture.md.
/// </summary>
public sealed class LicensePlan
{
    public Guid Id { get; set; }
    public string Name { get; set; } = default!;

    /// <summary>Module keys from ModuleCatalog (e.g. "org", "time", "leave").</summary>
    public string[] IncludedModules { get; set; } = [];

    /// <summary>Whether this plan can currently be assigned to new tenants.</summary>
    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; }
}

namespace WorkBase.Infrastructure.Persistence.Entities;

/// <summary>
/// Per-tenant key-value configuration. Mapped to cfg_tenant_configs table.
/// </summary>
public sealed class TenantConfig
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public string Key { get; set; } = default!;
    public string Value { get; set; } = default!;
    public DateTime UpdatedAt { get; set; }
}

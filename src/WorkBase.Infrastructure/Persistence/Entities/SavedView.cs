using WorkBase.Shared.Domain;

namespace WorkBase.Infrastructure.Persistence.Entities;

/// <summary>
/// User-saved filter/view configuration for a specific entity list.
/// Mapped to cfg_saved_views table.
/// </summary>
public sealed class SavedView : ITenantScoped
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public string UserId { get; set; } = default!;
    public string EntityType { get; set; } = default!;
    public string Name { get; set; } = default!;
    public string FiltersJson { get; set; } = "{}";
    public string SortJson { get; set; } = "{}";
    public string? ColumnsJson { get; set; }
    public bool IsDefault { get; set; }
    public bool IsShared { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

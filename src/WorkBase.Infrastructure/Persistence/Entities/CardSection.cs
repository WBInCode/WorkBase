using WorkBase.Shared.Domain;

namespace WorkBase.Infrastructure.Persistence.Entities;

/// <summary>
/// Groups custom fields into named sections on entity cards.
/// Mapped to cfg_card_sections table.
/// </summary>
public sealed class CardSection : ITenantScoped
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public string EntityType { get; set; } = default!;
    public string SectionName { get; set; } = default!;
    public string? Icon { get; set; }
    public int SortOrder { get; set; }
    public bool IsCollapsedByDefault { get; set; }
    public DateTime CreatedAt { get; set; }
}

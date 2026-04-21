using WorkBase.Shared.Domain;

namespace WorkBase.Infrastructure.Persistence.Entities;

/// <summary>
/// Defines a custom field for a specific entity type within a tenant.
/// Mapped to cfg_custom_field_definitions table.
/// </summary>
public sealed class CustomFieldDefinition : ITenantScoped
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public string EntityType { get; set; } = default!;
    public string FieldName { get; set; } = default!;
    public string FieldType { get; set; } = default!; // text, number, date, boolean, select
    public bool IsRequired { get; set; }
    public string? Options { get; set; } // JSON array for 'select' type
    public int SortOrder { get; set; }
    public Guid? SectionId { get; set; }
    public DateTime CreatedAt { get; set; }
}

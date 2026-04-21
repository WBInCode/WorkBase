using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WorkBase.Infrastructure.Persistence.Entities;

namespace WorkBase.Infrastructure.Persistence.Configurations;

public sealed class CustomFieldDefinitionConfiguration : IEntityTypeConfiguration<CustomFieldDefinition>
{
    public void Configure(EntityTypeBuilder<CustomFieldDefinition> builder)
    {
        builder.ToTable("cfg_custom_field_definitions");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.TenantId).HasColumnName("tenant_id").IsRequired();
        builder.Property(x => x.EntityType).HasColumnName("entity_type").HasMaxLength(128).IsRequired();
        builder.Property(x => x.FieldName).HasColumnName("field_name").HasMaxLength(128).IsRequired();
        builder.Property(x => x.FieldType).HasColumnName("field_type").HasMaxLength(32).IsRequired();
        builder.Property(x => x.IsRequired).HasColumnName("is_required").HasDefaultValue(false);
        builder.Property(x => x.Options).HasColumnName("options").HasColumnType("jsonb");
        builder.Property(x => x.SortOrder).HasColumnName("sort_order").HasDefaultValue(0);
        builder.Property(x => x.CreatedAt).HasColumnName("created_at").IsRequired();

        builder.HasIndex(x => new { x.TenantId, x.EntityType, x.FieldName }).IsUnique();
    }
}

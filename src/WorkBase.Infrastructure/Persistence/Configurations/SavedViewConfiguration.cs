using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WorkBase.Infrastructure.Persistence.Entities;

namespace WorkBase.Infrastructure.Persistence.Configurations;

public sealed class SavedViewConfiguration : IEntityTypeConfiguration<SavedView>
{
    public void Configure(EntityTypeBuilder<SavedView> builder)
    {
        builder.ToTable("cfg_saved_views");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.TenantId).IsRequired();
        builder.Property(e => e.UserId).IsRequired().HasMaxLength(256);
        builder.Property(e => e.EntityType).IsRequired().HasMaxLength(128);
        builder.Property(e => e.Name).IsRequired().HasMaxLength(256);
        builder.Property(e => e.FiltersJson).HasColumnType("jsonb");
        builder.Property(e => e.SortJson).HasColumnType("jsonb");
        builder.Property(e => e.ColumnsJson).HasColumnType("jsonb");
        builder.HasIndex(e => new { e.TenantId, e.UserId, e.EntityType });
    }
}

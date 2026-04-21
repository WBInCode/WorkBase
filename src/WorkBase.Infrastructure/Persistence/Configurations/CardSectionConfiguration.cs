using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WorkBase.Infrastructure.Persistence.Entities;

namespace WorkBase.Infrastructure.Persistence.Configurations;

public sealed class CardSectionConfiguration : IEntityTypeConfiguration<CardSection>
{
    public void Configure(EntityTypeBuilder<CardSection> builder)
    {
        builder.ToTable("cfg_card_sections");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.TenantId).IsRequired();
        builder.Property(e => e.EntityType).IsRequired().HasMaxLength(128);
        builder.Property(e => e.SectionName).IsRequired().HasMaxLength(256);
        builder.Property(e => e.Icon).HasMaxLength(64);
        builder.HasIndex(e => new { e.TenantId, e.EntityType });
    }
}

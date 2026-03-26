using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WorkBase.Modules.Identity.Domain.Entities;

namespace WorkBase.Modules.Identity.Infrastructure.Persistence;

public sealed class FeatureFlagConfiguration : IEntityTypeConfiguration<FeatureFlag>
{
    public void Configure(EntityTypeBuilder<FeatureFlag> builder)
    {
        builder.ToTable("iam_feature_flags");

        builder.HasKey(ff => ff.Id);

        builder.Property(ff => ff.TenantId)
            .IsRequired();

        builder.Property(ff => ff.Module)
            .IsRequired()
            .HasMaxLength(64);

        builder.Property(ff => ff.IsEnabled)
            .IsRequired()
            .HasDefaultValue(true);

        builder.Property(ff => ff.EnabledBy)
            .HasMaxLength(256);

        builder.HasIndex(ff => ff.TenantId);

        builder.HasIndex(ff => new { ff.TenantId, ff.Module })
            .IsUnique();

        builder.Ignore(ff => ff.DomainEvents);
    }
}

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WorkBase.Modules.TimeTracking.Domain.Entities;

namespace WorkBase.Modules.TimeTracking.Infrastructure.Persistence;

public sealed class QrTokenConfiguration : IEntityTypeConfiguration<QrToken>
{
    public void Configure(EntityTypeBuilder<QrToken> builder)
    {
        builder.ToTable("time_qr_tokens");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.TenantId)
            .IsRequired();

        builder.Property(e => e.Token)
            .IsRequired()
            .HasMaxLength(256);

        builder.Property(e => e.ExpiresAt)
            .IsRequired();

        builder.Property(e => e.LocationId)
            .HasMaxLength(128);

        builder.Property(e => e.IsUsed)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(e => e.CreatedAt)
            .IsRequired();

        builder.HasIndex(e => e.TenantId);

        builder.HasIndex(e => e.Token)
            .IsUnique();

        builder.HasIndex(e => new { e.TenantId, e.ExpiresAt });
    }
}

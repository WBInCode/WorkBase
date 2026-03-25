using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WorkBase.Shared.Domain;

namespace WorkBase.Infrastructure.Persistence;

public sealed class AuditEntryConfiguration : IEntityTypeConfiguration<AuditEntry>
{
    public void Configure(EntityTypeBuilder<AuditEntry> builder)
    {
        builder.ToTable("audit_entries");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.EntityType)
            .IsRequired()
            .HasMaxLength(256);

        builder.Property(e => e.EntityId)
            .IsRequired()
            .HasMaxLength(256);

        builder.Property(e => e.Action)
            .IsRequired()
            .HasMaxLength(16);

        builder.Property(e => e.OldValues)
            .HasColumnType("jsonb");

        builder.Property(e => e.NewValues)
            .HasColumnType("jsonb");

        builder.Property(e => e.ChangedColumns)
            .HasColumnType("jsonb");

        builder.Property(e => e.UserId)
            .HasMaxLength(256);

        builder.Property(e => e.Timestamp)
            .IsRequired();

        builder.HasIndex(e => e.EntityType);
        builder.HasIndex(e => new { e.EntityType, e.EntityId });
        builder.HasIndex(e => e.TenantId);
        builder.HasIndex(e => e.Timestamp);
    }
}

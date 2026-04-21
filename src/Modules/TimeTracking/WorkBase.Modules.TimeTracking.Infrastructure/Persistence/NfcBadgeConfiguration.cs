using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WorkBase.Modules.TimeTracking.Domain.Entities;

namespace WorkBase.Modules.TimeTracking.Infrastructure.Persistence;

public sealed class NfcBadgeConfiguration : IEntityTypeConfiguration<NfcBadge>
{
    public void Configure(EntityTypeBuilder<NfcBadge> builder)
    {
        builder.ToTable("time_nfc_badges");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.TenantId).IsRequired();
        builder.Property(e => e.EmployeeId).IsRequired();
        builder.Property(e => e.BadgeUid).IsRequired().HasMaxLength(128);
        builder.Property(e => e.Label).HasMaxLength(256);
        builder.HasIndex(e => new { e.TenantId, e.BadgeUid }).IsUnique();
        builder.HasIndex(e => e.EmployeeId);
    }
}

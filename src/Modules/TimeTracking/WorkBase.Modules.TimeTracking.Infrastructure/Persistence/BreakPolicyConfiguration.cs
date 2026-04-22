using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WorkBase.Modules.TimeTracking.Domain.Entities;

namespace WorkBase.Modules.TimeTracking.Infrastructure.Persistence;

public sealed class BreakPolicyConfiguration : IEntityTypeConfiguration<BreakPolicy>
{
    public void Configure(EntityTypeBuilder<BreakPolicy> builder)
    {
        builder.ToTable("time_break_policies");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.TenantId).IsRequired();
        builder.Property(e => e.Name).IsRequired().HasMaxLength(128);
        builder.Property(e => e.BreakType).IsRequired().HasConversion<string>().HasMaxLength(32);
        builder.Property(e => e.MaxPerDay);
        builder.Property(e => e.MaxMinutesPerBreak);
        builder.Property(e => e.MaxMinutesPerDay);
        builder.Property(e => e.IsActive).IsRequired();

        builder.HasIndex(e => e.TenantId);
        builder.HasIndex(e => new { e.TenantId, e.BreakType });
    }
}

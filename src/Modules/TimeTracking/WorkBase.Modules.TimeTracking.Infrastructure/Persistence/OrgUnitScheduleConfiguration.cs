using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WorkBase.Modules.TimeTracking.Domain.Entities;

namespace WorkBase.Modules.TimeTracking.Infrastructure.Persistence;

public sealed class OrgUnitScheduleConfiguration : IEntityTypeConfiguration<OrgUnitSchedule>
{
    public void Configure(EntityTypeBuilder<OrgUnitSchedule> builder)
    {
        builder.ToTable("time_org_unit_schedules");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.TenantId).IsRequired();
        builder.Property(e => e.OrgUnitId).IsRequired();
        builder.Property(e => e.Name).IsRequired().HasMaxLength(200);
        builder.Property(e => e.WeekPattern).IsRequired().HasColumnType("jsonb");
        builder.Property(e => e.EffectiveFrom).IsRequired();
        builder.Property(e => e.IsActive).IsRequired().HasDefaultValue(true);

        builder.HasIndex(e => e.TenantId);
        builder.HasIndex(e => new { e.TenantId, e.OrgUnitId }).IsUnique();
    }
}

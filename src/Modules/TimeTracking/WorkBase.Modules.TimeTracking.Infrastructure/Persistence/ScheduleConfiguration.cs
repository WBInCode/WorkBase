using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WorkBase.Modules.TimeTracking.Domain.Entities;

namespace WorkBase.Modules.TimeTracking.Infrastructure.Persistence;

public sealed class ScheduleConfiguration : IEntityTypeConfiguration<Schedule>
{
    public void Configure(EntityTypeBuilder<Schedule> builder)
    {
        builder.ToTable("time_schedules");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.TenantId)
            .IsRequired();

        builder.Property(e => e.EmployeeId)
            .IsRequired();

        builder.Property(e => e.Date)
            .IsRequired();

        builder.Property(e => e.PlannedStart)
            .IsRequired();

        builder.Property(e => e.PlannedEnd)
            .IsRequired();

        builder.Property(e => e.ShiftType)
            .HasMaxLength(64);

        builder.HasOne<ScheduleTemplate>()
            .WithMany()
            .HasForeignKey(e => e.TemplateId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.Property(e => e.Source)
            .IsRequired()
            .HasDefaultValue(ScheduleSource.Individual)
            .HasSentinel(ScheduleSource.Individual)
            .HasConversion<int>();

        builder.HasOne<OrgUnitSchedule>()
            .WithMany()
            .HasForeignKey(e => e.OrgUnitScheduleId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(e => e.TenantId);

        builder.HasIndex(e => new { e.TenantId, e.EmployeeId, e.Date })
            .IsUnique();

        builder.HasIndex(e => new { e.EmployeeId, e.Date });

        builder.HasIndex(e => new { e.TenantId, e.OrgUnitScheduleId, e.Source });
    }
}

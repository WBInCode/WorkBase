using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WorkBase.Modules.Leave.Domain.Entities;

namespace WorkBase.Modules.Leave.Infrastructure.Persistence;

public sealed class LeaveCalendarEntryConfiguration : IEntityTypeConfiguration<LeaveCalendarEntry>
{
    public void Configure(EntityTypeBuilder<LeaveCalendarEntry> builder)
    {
        builder.ToTable("leave_calendar_entries");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.TenantId)
            .IsRequired();

        builder.Property(e => e.EmployeeId)
            .IsRequired();

        builder.Property(e => e.LeaveRequestId)
            .IsRequired();

        builder.Property(e => e.LeaveTypeId)
            .IsRequired();

        builder.Property(e => e.Date)
            .IsRequired();

        builder.Property(e => e.DayFraction)
            .IsRequired()
            .HasPrecision(3, 2);

        builder.HasIndex(e => e.TenantId);

        builder.HasIndex(e => new { e.TenantId, e.EmployeeId, e.Date });

        builder.HasIndex(e => new { e.TenantId, e.Date });

        builder.HasIndex(e => new { e.TenantId, e.LeaveRequestId });
    }
}

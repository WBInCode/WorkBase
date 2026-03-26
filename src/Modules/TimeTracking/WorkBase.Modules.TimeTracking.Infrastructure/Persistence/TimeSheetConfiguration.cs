using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WorkBase.Modules.TimeTracking.Domain.Entities;

namespace WorkBase.Modules.TimeTracking.Infrastructure.Persistence;

public sealed class TimeSheetConfiguration : IEntityTypeConfiguration<TimeSheet>
{
    public void Configure(EntityTypeBuilder<TimeSheet> builder)
    {
        builder.ToTable("time_sheets");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.TenantId)
            .IsRequired();

        builder.Property(e => e.EmployeeId)
            .IsRequired();

        builder.Property(e => e.Date)
            .IsRequired();

        builder.Property(e => e.TotalWorked)
            .IsRequired();

        builder.Property(e => e.TotalBreaks)
            .IsRequired();

        builder.Property(e => e.NetWorked)
            .IsRequired();

        builder.Property(e => e.Status)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(32);

        builder.Property(e => e.Note)
            .HasMaxLength(512);

        builder.HasIndex(e => e.TenantId);

        builder.HasIndex(e => new { e.TenantId, e.EmployeeId, e.Date })
            .IsUnique();

        builder.HasIndex(e => new { e.EmployeeId, e.Date });
    }
}

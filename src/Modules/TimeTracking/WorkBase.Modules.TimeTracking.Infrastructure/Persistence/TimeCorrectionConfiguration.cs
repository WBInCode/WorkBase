using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WorkBase.Modules.TimeTracking.Domain.Entities;

namespace WorkBase.Modules.TimeTracking.Infrastructure.Persistence;

public sealed class TimeCorrectionConfiguration : IEntityTypeConfiguration<TimeCorrection>
{
    public void Configure(EntityTypeBuilder<TimeCorrection> builder)
    {
        builder.ToTable("time_corrections");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.TenantId)
            .IsRequired();

        builder.Property(e => e.EmployeeId)
            .IsRequired();

        builder.Property(e => e.Date)
            .IsRequired();

        builder.Property(e => e.OriginalClockIn)
            .IsRequired();

        builder.Property(e => e.OriginalClockOut)
            .IsRequired();

        builder.Property(e => e.CorrectedClockIn)
            .IsRequired();

        builder.Property(e => e.CorrectedClockOut)
            .IsRequired();

        builder.Property(e => e.Reason)
            .IsRequired()
            .HasMaxLength(512);

        builder.Property(e => e.CorrectedBy)
            .IsRequired()
            .HasMaxLength(256);

        builder.Property(e => e.Status)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(32);

        builder.HasOne<TimeSheet>()
            .WithMany()
            .HasForeignKey(e => e.TimeSheetId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(e => e.TenantId);

        builder.HasIndex(e => new { e.TenantId, e.EmployeeId, e.Date });
    }
}

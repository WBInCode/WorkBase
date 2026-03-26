using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WorkBase.Modules.TimeTracking.Domain.Entities;

namespace WorkBase.Modules.TimeTracking.Infrastructure.Persistence;

public sealed class TimeAnomalyConfiguration : IEntityTypeConfiguration<TimeAnomaly>
{
    public void Configure(EntityTypeBuilder<TimeAnomaly> builder)
    {
        builder.ToTable("time_anomalies");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.TenantId)
            .IsRequired();

        builder.Property(e => e.EmployeeId)
            .IsRequired();

        builder.Property(e => e.Date)
            .IsRequired();

        builder.Property(e => e.Type)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(32);

        builder.Property(e => e.Status)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(32);

        builder.Property(e => e.Description)
            .HasMaxLength(512);

        builder.Property(e => e.Details)
            .HasColumnType("jsonb");

        builder.Property(e => e.ReviewedBy)
            .HasMaxLength(256);

        builder.HasOne<TimeSheet>()
            .WithMany()
            .HasForeignKey(e => e.TimeSheetId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(e => e.TenantId);

        builder.HasIndex(e => new { e.TenantId, e.EmployeeId, e.Date });

        builder.HasIndex(e => new { e.TenantId, e.Status });
    }
}

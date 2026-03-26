using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WorkBase.Modules.TimeTracking.Domain.Entities;

namespace WorkBase.Modules.TimeTracking.Infrastructure.Persistence;

public sealed class TimeEntryConfiguration : IEntityTypeConfiguration<TimeEntry>
{
    public void Configure(EntityTypeBuilder<TimeEntry> builder)
    {
        builder.ToTable("time_entries");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.TenantId)
            .IsRequired();

        builder.Property(e => e.EmployeeId)
            .IsRequired();

        builder.Property(e => e.EntryTime)
            .IsRequired();

        builder.Property(e => e.Type)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(32);

        builder.Property(e => e.Method)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(32);

        builder.Property(e => e.Note)
            .HasMaxLength(512);

        builder.Property(e => e.IpAddress)
            .HasMaxLength(45);

        builder.Property(e => e.Location)
            .HasMaxLength(256);

        builder.HasIndex(e => e.TenantId);

        builder.HasIndex(e => new { e.TenantId, e.EmployeeId, e.EntryTime });

        builder.HasIndex(e => new { e.EmployeeId, e.EntryTime });
    }
}

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WorkBase.Modules.Leave.Domain.Entities;

namespace WorkBase.Modules.Leave.Infrastructure.Persistence;

public sealed class LeaveTypeConfiguration : IEntityTypeConfiguration<LeaveType>
{
    public void Configure(EntityTypeBuilder<LeaveType> builder)
    {
        builder.ToTable("leave_types");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.TenantId)
            .IsRequired();

        builder.Property(e => e.Code)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(e => e.Name)
            .IsRequired()
            .HasMaxLength(128);

        builder.Property(e => e.Description)
            .HasMaxLength(512);

        builder.Property(e => e.IsPaid)
            .IsRequired();

        builder.Property(e => e.RequiresApproval)
            .IsRequired();

        builder.Property(e => e.Color)
            .HasMaxLength(32);

        builder.Property(e => e.IsActive)
            .IsRequired()
            .HasDefaultValue(true);

        builder.Property(e => e.SortOrder)
            .IsRequired();

        builder.HasIndex(e => e.TenantId);

        builder.HasIndex(e => new { e.TenantId, e.Code })
            .IsUnique();
    }
}

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WorkBase.Modules.Leave.Domain.Entities;

namespace WorkBase.Modules.Leave.Infrastructure.Persistence;

public sealed class LeaveBalanceConfiguration : IEntityTypeConfiguration<LeaveBalance>
{
    public void Configure(EntityTypeBuilder<LeaveBalance> builder)
    {
        builder.ToTable("leave_balances");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.TenantId)
            .IsRequired();

        builder.Property(e => e.EmployeeId)
            .IsRequired();

        builder.Property(e => e.LeaveTypeId)
            .IsRequired();

        builder.Property(e => e.Year)
            .IsRequired();

        builder.Property(e => e.TotalDays)
            .IsRequired()
            .HasPrecision(5, 1);

        builder.Property(e => e.UsedDays)
            .IsRequired()
            .HasPrecision(5, 1);

        builder.Property(e => e.PendingDays)
            .IsRequired()
            .HasPrecision(5, 1);

        builder.Property(e => e.CarriedOverDays)
            .IsRequired()
            .HasPrecision(5, 1);

        builder.Ignore(e => e.RemainingDays);

        builder.HasIndex(e => e.TenantId);

        builder.HasIndex(e => new { e.TenantId, e.EmployeeId, e.LeaveTypeId, e.Year })
            .IsUnique();
    }
}

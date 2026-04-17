using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WorkBase.Modules.Leave.Domain.Entities;

namespace WorkBase.Modules.Leave.Infrastructure.Persistence;

public sealed class LeaveDecisionConfiguration : IEntityTypeConfiguration<LeaveDecision>
{
    public void Configure(EntityTypeBuilder<LeaveDecision> builder)
    {
        builder.ToTable("leave_decisions");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.TenantId)
            .IsRequired();

        builder.Property(e => e.LeaveRequestId)
            .IsRequired();

        builder.Property(e => e.DecidedByEmployeeId)
            .IsRequired();

        builder.Property(e => e.Decision)
            .IsRequired()
            .HasMaxLength(32);

        builder.Property(e => e.Comment)
            .HasMaxLength(1024);

        builder.Property(e => e.DecidedAt)
            .IsRequired();

        builder.HasIndex(e => new { e.TenantId, e.LeaveRequestId });
    }
}

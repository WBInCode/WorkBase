using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WorkBase.Modules.Workflow.Domain.Entities;

namespace WorkBase.Modules.Workflow.Infrastructure.Persistence;

public sealed class ApprovalRequestConfiguration : IEntityTypeConfiguration<ApprovalRequest>
{
    public void Configure(EntityTypeBuilder<ApprovalRequest> builder)
    {
        builder.ToTable("wf_approval_requests");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.TenantId)
            .IsRequired();

        builder.Property(e => e.StepId)
            .IsRequired();

        builder.Property(e => e.InstanceId)
            .IsRequired();

        builder.Property(e => e.RequesterId)
            .IsRequired();

        builder.Property(e => e.ApproverId)
            .IsRequired();

        builder.Property(e => e.Status)
            .IsRequired()
            .HasMaxLength(32);

        builder.Property(e => e.Order)
            .IsRequired();

        builder.HasIndex(e => new { e.TenantId, e.InstanceId });

        builder.HasIndex(e => new { e.TenantId, e.ApproverId, e.Status });

        builder.HasIndex(e => new { e.TenantId, e.StepId });
    }
}

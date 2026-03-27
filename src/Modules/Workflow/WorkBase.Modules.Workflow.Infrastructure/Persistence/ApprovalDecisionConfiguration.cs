using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WorkBase.Modules.Workflow.Domain.Entities;

namespace WorkBase.Modules.Workflow.Infrastructure.Persistence;

public sealed class ApprovalDecisionConfiguration : IEntityTypeConfiguration<ApprovalDecision>
{
    public void Configure(EntityTypeBuilder<ApprovalDecision> builder)
    {
        builder.ToTable("wf_approval_decisions");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.TenantId)
            .IsRequired();

        builder.Property(e => e.RequestId)
            .IsRequired();

        builder.Property(e => e.DecidedBy)
            .IsRequired();

        builder.Property(e => e.Decision)
            .IsRequired()
            .HasMaxLength(32);

        builder.Property(e => e.Comment)
            .HasMaxLength(1024);

        builder.Property(e => e.DecidedAt)
            .IsRequired();

        builder.HasIndex(e => new { e.TenantId, e.RequestId });
    }
}

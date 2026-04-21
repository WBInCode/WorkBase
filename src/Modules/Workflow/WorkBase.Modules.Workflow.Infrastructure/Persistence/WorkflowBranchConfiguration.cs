using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WorkBase.Modules.Workflow.Domain.Entities;

namespace WorkBase.Modules.Workflow.Infrastructure.Persistence;

public sealed class WorkflowBranchConfiguration : IEntityTypeConfiguration<WorkflowBranch>
{
    public void Configure(EntityTypeBuilder<WorkflowBranch> builder)
    {
        builder.ToTable("wf_branches");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.TenantId).IsRequired();
        builder.Property(e => e.InstanceId).IsRequired();
        builder.Property(e => e.GatewayStepName).IsRequired().HasMaxLength(128);
        builder.Property(e => e.BranchName).IsRequired().HasMaxLength(128);
        builder.Property(e => e.Status).IsRequired().HasMaxLength(32);
        builder.Property(e => e.CurrentStepName).HasMaxLength(128);

        builder.HasIndex(e => new { e.InstanceId, e.GatewayStepName });
        builder.HasIndex(e => e.TenantId);
    }
}

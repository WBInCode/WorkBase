using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WorkBase.Modules.Workflow.Domain.Entities;

namespace WorkBase.Modules.Workflow.Infrastructure.Persistence;

public sealed class WorkflowStepConfiguration : IEntityTypeConfiguration<WorkflowStep>
{
    public void Configure(EntityTypeBuilder<WorkflowStep> builder)
    {
        builder.ToTable("wf_steps");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.TenantId)
            .IsRequired();

        builder.Property(e => e.InstanceId)
            .IsRequired();

        builder.Property(e => e.StepName)
            .IsRequired()
            .HasMaxLength(128);

        builder.Property(e => e.Status)
            .IsRequired()
            .HasMaxLength(32);

        builder.Property(e => e.CompletedBy)
            .HasMaxLength(256);

        builder.Property(e => e.Outcome)
            .HasMaxLength(64);

        builder.Property(e => e.Comment)
            .HasMaxLength(1024);

        builder.HasIndex(e => new { e.TenantId, e.InstanceId });
    }
}

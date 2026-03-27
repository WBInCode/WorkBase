using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WorkBase.Modules.Workflow.Domain.Entities;

namespace WorkBase.Modules.Workflow.Infrastructure.Persistence;

public sealed class WorkflowActionConfiguration : IEntityTypeConfiguration<WorkflowAction>
{
    public void Configure(EntityTypeBuilder<WorkflowAction> builder)
    {
        builder.ToTable("wf_actions");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.TenantId)
            .IsRequired();

        builder.Property(e => e.StepId)
            .IsRequired();

        builder.Property(e => e.InstanceId)
            .IsRequired();

        builder.Property(e => e.ActionType)
            .IsRequired()
            .HasMaxLength(64);

        builder.Property(e => e.PayloadJson)
            .HasColumnType("jsonb");

        builder.Property(e => e.Status)
            .IsRequired()
            .HasMaxLength(32);

        builder.Property(e => e.ExecutedAt)
            .IsRequired();

        builder.Property(e => e.ErrorMessage)
            .HasMaxLength(1024);

        builder.HasIndex(e => new { e.TenantId, e.InstanceId });

        builder.HasIndex(e => new { e.TenantId, e.StepId });
    }
}

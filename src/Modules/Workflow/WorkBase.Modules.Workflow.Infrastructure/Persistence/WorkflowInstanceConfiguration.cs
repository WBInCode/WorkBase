using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WorkBase.Modules.Workflow.Domain.Entities;

namespace WorkBase.Modules.Workflow.Infrastructure.Persistence;

public sealed class WorkflowInstanceConfiguration : IEntityTypeConfiguration<WorkflowInstance>
{
    public void Configure(EntityTypeBuilder<WorkflowInstance> builder)
    {
        builder.ToTable("wf_instances");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.TenantId)
            .IsRequired();

        builder.Property(e => e.DefinitionId)
            .IsRequired();

        builder.Property(e => e.EntityType)
            .IsRequired()
            .HasMaxLength(64);

        builder.Property(e => e.EntityId)
            .IsRequired();

        builder.Property(e => e.CurrentStepName)
            .IsRequired()
            .HasMaxLength(128);

        builder.Property(e => e.Status)
            .IsRequired()
            .HasMaxLength(32);

        builder.Property(e => e.InitiatedBy)
            .IsRequired();

        builder.HasIndex(e => e.TenantId);

        builder.HasIndex(e => new { e.TenantId, e.EntityType, e.EntityId });

        builder.HasIndex(e => new { e.TenantId, e.DefinitionId });

        builder.HasIndex(e => new { e.TenantId, e.Status });
    }
}

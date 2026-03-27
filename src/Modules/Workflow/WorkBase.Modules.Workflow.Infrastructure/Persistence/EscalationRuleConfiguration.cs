using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WorkBase.Modules.Workflow.Domain.Entities;

namespace WorkBase.Modules.Workflow.Infrastructure.Persistence;

public sealed class EscalationRuleConfiguration : IEntityTypeConfiguration<EscalationRule>
{
    public void Configure(EntityTypeBuilder<EscalationRule> builder)
    {
        builder.ToTable("wf_escalation_rules");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.TenantId)
            .IsRequired();

        builder.Property(e => e.DefinitionId)
            .IsRequired();

        builder.Property(e => e.StepName)
            .IsRequired()
            .HasMaxLength(128);

        builder.Property(e => e.TimeoutMinutes)
            .IsRequired();

        builder.Property(e => e.ActionType)
            .IsRequired()
            .HasMaxLength(64);

        builder.Property(e => e.ActionPayloadJson)
            .HasColumnType("jsonb");

        builder.Property(e => e.IsActive)
            .IsRequired()
            .HasDefaultValue(true);

        builder.HasIndex(e => new { e.TenantId, e.DefinitionId });
    }
}

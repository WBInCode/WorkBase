using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WorkBase.Modules.Tasks.Domain.Entities;

namespace WorkBase.Modules.Tasks.Infrastructure.Persistence;

public sealed class TaskStatusTransitionConfiguration : IEntityTypeConfiguration<TaskStatusTransition>
{
    public void Configure(EntityTypeBuilder<TaskStatusTransition> builder)
    {
        builder.ToTable("task_status_transitions");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.TenantId).IsRequired();
        builder.Property(e => e.FromStatusId).IsRequired();
        builder.Property(e => e.ToStatusId).IsRequired();

        builder.Property(e => e.IsActive)
            .IsRequired()
            .HasDefaultValue(true);

        builder.HasIndex(e => e.TenantId);
        builder.HasIndex(e => new { e.TenantId, e.FromStatusId, e.ToStatusId }).IsUnique();
    }
}

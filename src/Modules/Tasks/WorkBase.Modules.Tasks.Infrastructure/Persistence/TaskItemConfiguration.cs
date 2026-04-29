using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WorkBase.Modules.Tasks.Domain.Entities;

namespace WorkBase.Modules.Tasks.Infrastructure.Persistence;

public sealed class TaskItemConfiguration : IEntityTypeConfiguration<TaskItem>
{
    public void Configure(EntityTypeBuilder<TaskItem> builder)
    {
        builder.ToTable("task_tasks");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.TenantId).IsRequired();

        builder.Property(e => e.Title)
            .IsRequired()
            .HasMaxLength(256);

        builder.Property(e => e.Description)
            .HasMaxLength(4000);

        builder.Property(e => e.StatusId).IsRequired();
        builder.Property(e => e.PriorityId).IsRequired();
        builder.Property(e => e.AssigneeId).IsRequired();
        builder.Property(e => e.CoAssigneeId);

        builder.HasIndex(e => e.TenantId);
        builder.HasIndex(e => new { e.TenantId, e.AssigneeId });
        builder.HasIndex(e => new { e.TenantId, e.CoAssigneeId });
        builder.HasIndex(e => new { e.TenantId, e.StatusId });
    }
}

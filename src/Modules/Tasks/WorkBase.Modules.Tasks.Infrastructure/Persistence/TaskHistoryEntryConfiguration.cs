using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WorkBase.Modules.Tasks.Domain.Entities;

namespace WorkBase.Modules.Tasks.Infrastructure.Persistence;

public sealed class TaskHistoryEntryConfiguration : IEntityTypeConfiguration<TaskHistoryEntry>
{
    public void Configure(EntityTypeBuilder<TaskHistoryEntry> builder)
    {
        builder.ToTable("task_history");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.TenantId).IsRequired();
        builder.Property(e => e.TaskId).IsRequired();
        builder.Property(e => e.ChangedById).IsRequired();

        builder.Property(e => e.FieldName)
            .IsRequired()
            .HasMaxLength(128);

        builder.Property(e => e.OldValue)
            .HasMaxLength(1024);

        builder.Property(e => e.NewValue)
            .HasMaxLength(1024);

        builder.Property(e => e.ChangedAt).IsRequired();

        builder.HasIndex(e => e.TenantId);
        builder.HasIndex(e => new { e.TenantId, e.TaskId });
    }
}

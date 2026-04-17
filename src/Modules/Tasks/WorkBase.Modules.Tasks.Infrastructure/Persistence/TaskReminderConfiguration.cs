using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WorkBase.Modules.Tasks.Domain.Entities;

namespace WorkBase.Modules.Tasks.Infrastructure.Persistence;

public sealed class TaskReminderConfiguration : IEntityTypeConfiguration<TaskReminder>
{
    public void Configure(EntityTypeBuilder<TaskReminder> builder)
    {
        builder.ToTable("task_reminders");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.TenantId).IsRequired();
        builder.Property(e => e.TaskId).IsRequired();
        builder.Property(e => e.RecipientId).IsRequired();
        builder.Property(e => e.RemindAt).IsRequired();

        builder.Property(e => e.IsSent)
            .IsRequired()
            .HasDefaultValue(false);

        builder.HasIndex(e => e.TenantId);
        builder.HasIndex(e => new { e.TenantId, e.TaskId });
        builder.HasIndex(e => new { e.IsSent, e.RemindAt });
    }
}

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WorkBase.Modules.Tasks.Domain.Entities;

namespace WorkBase.Modules.Tasks.Infrastructure.Persistence;

public sealed class TaskAssigneeConfiguration : IEntityTypeConfiguration<TaskAssignee>
{
    public void Configure(EntityTypeBuilder<TaskAssignee> builder)
    {
        builder.ToTable("task_task_assignees");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.TaskId).IsRequired();
        builder.Property(e => e.EmployeeId).IsRequired();

        builder.HasIndex(e => new { e.TaskId, e.EmployeeId }).IsUnique();
        builder.HasIndex(e => e.EmployeeId);
    }
}

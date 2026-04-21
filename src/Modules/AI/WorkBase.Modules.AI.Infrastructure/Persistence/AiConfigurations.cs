using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WorkBase.Modules.AI.Domain.Entities;

namespace WorkBase.Modules.AI.Infrastructure.Persistence;

public sealed class AiTaskLogConfiguration : IEntityTypeConfiguration<AiTaskLog>
{
    public void Configure(EntityTypeBuilder<AiTaskLog> builder)
    {
        builder.ToTable("ai_task_logs");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.TenantId).IsRequired();
        builder.Property(e => e.UserId).IsRequired().HasMaxLength(256);
        builder.Property(e => e.TaskType).HasConversion<string>().HasMaxLength(32);
        builder.Property(e => e.Status).HasConversion<string>().HasMaxLength(32);
        builder.Property(e => e.InputJson).HasColumnType("jsonb");
        builder.Property(e => e.OutputJson).HasColumnType("jsonb");
        builder.Property(e => e.ModelName).HasMaxLength(64);
        builder.Property(e => e.ErrorMessage).HasMaxLength(1024);
        builder.HasIndex(e => new { e.TenantId, e.CreatedAt });
    }
}

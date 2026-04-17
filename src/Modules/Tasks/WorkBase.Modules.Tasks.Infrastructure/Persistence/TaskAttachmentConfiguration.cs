using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WorkBase.Modules.Tasks.Domain.Entities;

namespace WorkBase.Modules.Tasks.Infrastructure.Persistence;

public sealed class TaskAttachmentConfiguration : IEntityTypeConfiguration<TaskAttachment>
{
    public void Configure(EntityTypeBuilder<TaskAttachment> builder)
    {
        builder.ToTable("task_attachments");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.TenantId).IsRequired();
        builder.Property(e => e.TaskId).IsRequired();
        builder.Property(e => e.UploadedById).IsRequired();

        builder.Property(e => e.FileName)
            .IsRequired()
            .HasMaxLength(256);

        builder.Property(e => e.StoragePath)
            .IsRequired()
            .HasMaxLength(1024);

        builder.Property(e => e.ContentType)
            .IsRequired()
            .HasMaxLength(128);

        builder.Property(e => e.FileSizeBytes).IsRequired();
        builder.Property(e => e.UploadedAt).IsRequired();

        builder.HasIndex(e => e.TenantId);
        builder.HasIndex(e => new { e.TenantId, e.TaskId });
    }
}

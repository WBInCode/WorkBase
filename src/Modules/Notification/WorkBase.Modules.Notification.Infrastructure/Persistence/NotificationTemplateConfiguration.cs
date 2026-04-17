using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WorkBase.Modules.Notification.Domain.Entities;

namespace WorkBase.Modules.Notification.Infrastructure.Persistence;

public sealed class NotificationTemplateConfiguration : IEntityTypeConfiguration<NotificationTemplate>
{
    public void Configure(EntityTypeBuilder<NotificationTemplate> builder)
    {
        builder.ToTable("notif_templates");
        builder.HasKey(t => t.Id);
        builder.Property(t => t.TenantId).IsRequired();
        builder.Property(t => t.Code).IsRequired().HasMaxLength(64);
        builder.Property(t => t.Name).IsRequired().HasMaxLength(128);
        builder.Property(t => t.TitleTemplate).IsRequired().HasMaxLength(512);
        builder.Property(t => t.BodyTemplate).IsRequired().HasMaxLength(4096);
        builder.Property(t => t.Category).IsRequired().HasMaxLength(64);
        builder.Property(t => t.IsActive).IsRequired();
        builder.HasIndex(t => new { t.TenantId, t.Code }).IsUnique();
    }
}

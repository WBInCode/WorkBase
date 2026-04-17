using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WorkBase.Modules.Notification.Domain.Entities;

namespace WorkBase.Modules.Notification.Infrastructure.Persistence;

public sealed class NotificationConfiguration : IEntityTypeConfiguration<Domain.Entities.Notification>
{
    public void Configure(EntityTypeBuilder<Domain.Entities.Notification> builder)
    {
        builder.ToTable("notif_notifications");

        builder.HasKey(n => n.Id);

        builder.Property(n => n.TenantId).IsRequired();
        builder.Property(n => n.RecipientUserId).IsRequired();
        builder.Property(n => n.Title).IsRequired().HasMaxLength(256);
        builder.Property(n => n.Body).IsRequired().HasMaxLength(4096);
        builder.Property(n => n.Category).IsRequired().HasMaxLength(64);
        builder.Property(n => n.IsRead).IsRequired();
        builder.Property(n => n.ReadAt);
        builder.Property(n => n.ReferenceType).HasMaxLength(64);
        builder.Property(n => n.ReferenceId);

        builder.HasIndex(n => n.TenantId);
        builder.HasIndex(n => new { n.TenantId, n.RecipientUserId, n.IsRead });
        builder.HasIndex(n => new { n.TenantId, n.RecipientUserId, n.CreatedAt });
    }
}

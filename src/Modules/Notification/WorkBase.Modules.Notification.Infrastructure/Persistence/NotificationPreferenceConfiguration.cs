using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WorkBase.Modules.Notification.Domain.Entities;

namespace WorkBase.Modules.Notification.Infrastructure.Persistence;

public sealed class NotificationPreferenceConfiguration : IEntityTypeConfiguration<NotificationPreference>
{
    public void Configure(EntityTypeBuilder<NotificationPreference> builder)
    {
        builder.ToTable("notif_preferences");
        builder.HasKey(p => p.Id);
        builder.Property(p => p.TenantId).IsRequired();
        builder.Property(p => p.UserId).IsRequired();
        builder.Property(p => p.Category).IsRequired().HasMaxLength(64);
        builder.Property(p => p.InApp).IsRequired();
        builder.Property(p => p.Email).IsRequired();
        builder.HasIndex(p => new { p.TenantId, p.UserId, p.Category }).IsUnique();
    }
}

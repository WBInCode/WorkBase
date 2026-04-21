using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WorkBase.Modules.Notification.Domain.Entities;

namespace WorkBase.Modules.Notification.Infrastructure.Persistence;

public sealed class PushSubscriptionConfiguration : IEntityTypeConfiguration<PushSubscription>
{
    public void Configure(EntityTypeBuilder<PushSubscription> builder)
    {
        builder.ToTable("notif_push_subscriptions");
        builder.HasKey(s => s.Id);
        builder.Property(s => s.TenantId).IsRequired();
        builder.Property(s => s.UserId).IsRequired();
        builder.Property(s => s.Endpoint).IsRequired().HasMaxLength(2048);
        builder.Property(s => s.P256dh).IsRequired().HasMaxLength(512);
        builder.Property(s => s.Auth).IsRequired().HasMaxLength(512);
        builder.Property(s => s.DeviceInfo).HasMaxLength(256);
        builder.Property(s => s.CreatedAt).IsRequired();
        builder.HasIndex(s => new { s.TenantId, s.UserId });
        builder.HasIndex(s => new { s.TenantId, s.UserId, s.Endpoint }).IsUnique();
    }
}

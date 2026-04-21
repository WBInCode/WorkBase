using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace WorkBase.Infrastructure.PublicApi;

public sealed class ApiKeyConfiguration : IEntityTypeConfiguration<ApiKey>
{
    public void Configure(EntityTypeBuilder<ApiKey> builder)
    {
        builder.ToTable("infra_api_keys");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.TenantId).IsRequired();
        builder.Property(e => e.Name).IsRequired().HasMaxLength(256);
        builder.Property(e => e.KeyHash).IsRequired().HasMaxLength(128);
        builder.Property(e => e.KeyPrefix).IsRequired().HasMaxLength(16);
        builder.Property(e => e.Description).HasMaxLength(1000);
        builder.Property(e => e.AllowedIps).HasMaxLength(1000);
        builder.Property(e => e.ScopesJson).HasColumnType("jsonb");
        builder.HasIndex(e => e.KeyHash).IsUnique();
        builder.HasIndex(e => e.TenantId);
    }
}

public sealed class WebhookSubscriptionConfiguration : IEntityTypeConfiguration<WebhookSubscription>
{
    public void Configure(EntityTypeBuilder<WebhookSubscription> builder)
    {
        builder.ToTable("infra_webhook_subscriptions");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.TenantId).IsRequired();
        builder.Property(e => e.Name).IsRequired().HasMaxLength(256);
        builder.Property(e => e.Url).IsRequired().HasMaxLength(2048);
        builder.Property(e => e.Secret).HasMaxLength(256);
        builder.Property(e => e.EventsJson).IsRequired().HasColumnType("jsonb");
        builder.Property(e => e.LastDeliveryStatus).HasMaxLength(256);
        builder.HasIndex(e => e.TenantId);
    }
}

public sealed class WebhookDeliveryLogConfiguration : IEntityTypeConfiguration<WebhookDeliveryLog>
{
    public void Configure(EntityTypeBuilder<WebhookDeliveryLog> builder)
    {
        builder.ToTable("infra_webhook_delivery_logs");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.TenantId).IsRequired();
        builder.Property(e => e.EventType).IsRequired().HasMaxLength(128);
        builder.Property(e => e.PayloadJson).IsRequired().HasColumnType("jsonb");
        builder.Property(e => e.Url).IsRequired().HasMaxLength(2048);
        builder.Property(e => e.ResponseBody).HasMaxLength(4000);
        builder.Property(e => e.ErrorMessage).HasMaxLength(1000);
        builder.HasIndex(e => e.SubscriptionId);
        builder.HasIndex(e => e.TenantId);
    }
}

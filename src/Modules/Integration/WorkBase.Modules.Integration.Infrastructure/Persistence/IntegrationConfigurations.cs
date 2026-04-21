using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WorkBase.Modules.Integration.Domain.Entities;

namespace WorkBase.Modules.Integration.Infrastructure.Persistence;

internal sealed class IntegrationConnectionConfiguration : IEntityTypeConfiguration<IntegrationConnection>
{
    public void Configure(EntityTypeBuilder<IntegrationConnection> builder)
    {
        builder.ToTable("int_connections");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.TenantId).IsRequired();
        builder.Property(x => x.UserId).IsRequired();
        builder.Property(x => x.Provider).IsRequired().HasConversion<string>().HasMaxLength(50);
        builder.Property(x => x.ExternalAccountId).HasMaxLength(500);
        builder.Property(x => x.DisplayName).HasMaxLength(200);
        builder.Property(x => x.IsActive).IsRequired();

        builder.HasIndex(x => new { x.TenantId, x.UserId, x.Provider }).IsUnique();
    }
}

internal sealed class OAuthTokenConfiguration : IEntityTypeConfiguration<OAuthToken>
{
    public void Configure(EntityTypeBuilder<OAuthToken> builder)
    {
        builder.ToTable("int_oauth_tokens");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.TenantId).IsRequired();
        builder.Property(x => x.UserId).IsRequired();
        builder.Property(x => x.Provider).IsRequired().HasConversion<string>().HasMaxLength(50);
        builder.Property(x => x.EncryptedAccessToken).IsRequired();
        builder.Property(x => x.EncryptedRefreshToken);
        builder.Property(x => x.ExpiresAtUtc).IsRequired();
        builder.Property(x => x.Scopes).HasMaxLength(2000);
        builder.Property(x => x.Status).IsRequired().HasConversion<string>().HasMaxLength(20);

        builder.HasIndex(x => new { x.TenantId, x.UserId, x.Provider }).IsUnique();
    }
}

internal sealed class WebhookRegistrationConfiguration : IEntityTypeConfiguration<WebhookRegistration>
{
    public void Configure(EntityTypeBuilder<WebhookRegistration> builder)
    {
        builder.ToTable("int_webhook_registrations");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.TenantId).IsRequired();
        builder.Property(x => x.Provider).IsRequired().HasConversion<string>().HasMaxLength(50);
        builder.Property(x => x.WebhookUrl).IsRequired().HasMaxLength(2000);
        builder.Property(x => x.Secret).HasMaxLength(500);
        builder.Property(x => x.EventTypes).HasMaxLength(1000);
        builder.Property(x => x.IsActive).IsRequired();

        builder.HasIndex(x => new { x.TenantId, x.Provider });
    }
}

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WorkBase.Infrastructure.Persistence.Entities;

namespace WorkBase.Infrastructure.Persistence.Configurations;

public sealed class TenantBrandingConfiguration : IEntityTypeConfiguration<TenantBranding>
{
    public void Configure(EntityTypeBuilder<TenantBranding> builder)
    {
        builder.ToTable("cfg_tenant_branding");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.TenantId).IsRequired();
        builder.Property(e => e.PrimaryColor).HasMaxLength(16);
        builder.Property(e => e.SecondaryColor).HasMaxLength(16);
        builder.Property(e => e.AccentColor).HasMaxLength(16);
        builder.Property(e => e.AppName).HasMaxLength(128);
        builder.Property(e => e.CustomDomain).HasMaxLength(256);
        builder.Property(e => e.CustomCss).HasMaxLength(8192);
        builder.HasIndex(e => e.TenantId).IsUnique();
        builder.HasIndex(e => e.CustomDomain).IsUnique().HasFilter("custom_domain IS NOT NULL");
    }
}

public sealed class OnboardingRequestConfiguration : IEntityTypeConfiguration<OnboardingRequest>
{
    public void Configure(EntityTypeBuilder<OnboardingRequest> builder)
    {
        builder.ToTable("cfg_onboarding_requests");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.CompanyName).IsRequired().HasMaxLength(256);
        builder.Property(e => e.AdminEmail).IsRequired().HasMaxLength(256);
        builder.Property(e => e.AdminFullName).IsRequired().HasMaxLength(256);
        builder.Property(e => e.Phone).HasMaxLength(32);
        builder.Property(e => e.PlanId).IsRequired().HasMaxLength(64);
        builder.Property(e => e.Status).IsRequired().HasMaxLength(32);
        builder.Property(e => e.ErrorMessage).HasMaxLength(1024);
        builder.HasIndex(e => e.AdminEmail);
    }
}

public sealed class BillingSubscriptionConfiguration : IEntityTypeConfiguration<BillingSubscription>
{
    public void Configure(EntityTypeBuilder<BillingSubscription> builder)
    {
        builder.ToTable("billing_subscriptions");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.TenantId).IsRequired();
        builder.Property(e => e.PlanId).IsRequired().HasMaxLength(64);
        builder.Property(e => e.PlanName).IsRequired().HasMaxLength(128);
        builder.Property(e => e.StripeCustomerId).IsRequired().HasMaxLength(128);
        builder.Property(e => e.StripeSubscriptionId).IsRequired().HasMaxLength(128);
        builder.Property(e => e.Status).IsRequired().HasMaxLength(32);
        builder.Property(e => e.MonthlyPrice).HasPrecision(10, 2);
        builder.Property(e => e.Currency).HasMaxLength(8);
        builder.HasIndex(e => e.TenantId).IsUnique();
        builder.HasIndex(e => e.StripeSubscriptionId).IsUnique();
    }
}

public sealed class BillingInvoiceConfiguration : IEntityTypeConfiguration<BillingInvoice>
{
    public void Configure(EntityTypeBuilder<BillingInvoice> builder)
    {
        builder.ToTable("billing_invoices");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.TenantId).IsRequired();
        builder.Property(e => e.StripeInvoiceId).IsRequired().HasMaxLength(128);
        builder.Property(e => e.Number).IsRequired().HasMaxLength(64);
        builder.Property(e => e.AmountDue).HasPrecision(10, 2);
        builder.Property(e => e.AmountPaid).HasPrecision(10, 2);
        builder.Property(e => e.Currency).HasMaxLength(8);
        builder.Property(e => e.Status).IsRequired().HasMaxLength(32);
        builder.Property(e => e.PdfUrl).HasMaxLength(512);
        builder.HasIndex(e => e.TenantId);
    }
}

using WorkBase.Shared.Domain;

namespace WorkBase.Infrastructure.Persistence.Entities;

/// <summary>
/// Tenant branding configuration for white-label support.
/// Table: cfg_tenant_branding
/// </summary>
public sealed class TenantBranding : ITenantScoped
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public string? LogoUrl { get; set; }
    public string? FaviconUrl { get; set; }
    public string PrimaryColor { get; set; } = "#3b82f6";
    public string SecondaryColor { get; set; } = "#1e40af";
    public string? AccentColor { get; set; }
    public string? AppName { get; set; }
    public string? CustomDomain { get; set; }
    public string? CustomCss { get; set; }
    public string? LoginBackgroundUrl { get; set; }
    public string? FooterHtml { get; set; }
    public DateTime UpdatedAt { get; set; }
}

/// <summary>
/// Self-service tenant onboarding request.
/// Table: cfg_onboarding_requests
/// </summary>
public sealed class OnboardingRequest
{
    public Guid Id { get; set; }
    public string CompanyName { get; set; } = default!;
    public string AdminEmail { get; set; } = default!;
    public string AdminFullName { get; set; } = default!;
    public string? Phone { get; set; }
    public string PlanId { get; set; } = default!;
    public string Status { get; set; } = "pending"; // pending, provisioning, active, failed
    public Guid? TenantId { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
}

/// <summary>
/// Billing subscription linked to a tenant.
/// Table: billing_subscriptions
/// </summary>
public sealed class BillingSubscription : ITenantScoped
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public string PlanId { get; set; } = default!;
    public string PlanName { get; set; } = default!;
    public string StripeCustomerId { get; set; } = default!;
    public string StripeSubscriptionId { get; set; } = default!;
    public string Status { get; set; } = "active"; // active, past_due, canceled, trialing
    public decimal MonthlyPrice { get; set; }
    public string Currency { get; set; } = "PLN";
    public int MaxUsers { get; set; }
    public DateTime CurrentPeriodStart { get; set; }
    public DateTime CurrentPeriodEnd { get; set; }
    public DateTime? CanceledAt { get; set; }
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// Billing invoice record.
/// Table: billing_invoices
/// </summary>
public sealed class BillingInvoice : ITenantScoped
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public string StripeInvoiceId { get; set; } = default!;
    public string Number { get; set; } = default!;
    public decimal AmountDue { get; set; }
    public decimal AmountPaid { get; set; }
    public string Currency { get; set; } = "PLN";
    public string Status { get; set; } = "draft"; // draft, open, paid, void, uncollectible
    public string? PdfUrl { get; set; }
    public DateTime PeriodStart { get; set; }
    public DateTime PeriodEnd { get; set; }
    public DateTime? PaidAt { get; set; }
    public DateTime CreatedAt { get; set; }
}

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Stripe;
using WorkBase.Infrastructure.Persistence;
using WorkBase.Infrastructure.Persistence.Entities;
using WorkBase.Shared.Api;
using WorkBase.Shared.Auth;

namespace WorkBase.Host.Endpoints;

public static class BrandingEndpoints
{
    public static IEndpointRouteBuilder MapBrandingEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/config/branding").WithTags("Branding").RequireAuthorization();

        group.MapGet("/", async (WorkBaseDbContext db, HttpContext http) =>
        {
            var tenantId = http.User.GetTenantId();
            if (tenantId is null) return Results.Forbid();
            var branding = await db.Set<TenantBranding>().FirstOrDefaultAsync(b => b.TenantId == tenantId.Value);
            return branding is not null ? Results.Ok(branding) : Results.Ok(new { PrimaryColor = "#3b82f6", SecondaryColor = "#1e40af" });
        }).WithName("GetBranding").WithSummary("Pobierz branding tenanta");

        group.MapPut("/", async (UpdateBrandingRequest req, WorkBaseDbContext db, HttpContext http) =>
        {
            var tenantId = http.User.GetTenantId();
            if (tenantId is null) return Results.Forbid();
            var branding = await db.Set<TenantBranding>().FirstOrDefaultAsync(b => b.TenantId == tenantId.Value);
            if (branding is null)
            {
                branding = new TenantBranding { Id = Guid.NewGuid(), TenantId = tenantId.Value };
                db.Set<TenantBranding>().Add(branding);
            }
            branding.LogoUrl = req.LogoUrl;
            branding.FaviconUrl = req.FaviconUrl;
            branding.PrimaryColor = req.PrimaryColor;
            branding.SecondaryColor = req.SecondaryColor;
            branding.AccentColor = req.AccentColor;
            branding.AppName = req.AppName;
            branding.CustomDomain = req.CustomDomain;
            branding.CustomCss = req.CustomCss;
            branding.LoginBackgroundUrl = req.LoginBackgroundUrl;
            branding.FooterHtml = req.FooterHtml;
            branding.UpdatedAt = DateTime.UtcNow;
            await db.SaveChangesAsync();
            return Results.Ok(branding);
        }).WithName("UpdateBranding").WithSummary("Aktualizuj branding").RequirePermission("config.manage");

        return endpoints;
    }
}

public static class OnboardingEndpoints
{
    public static IEndpointRouteBuilder MapOnboardingEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/onboarding").WithTags("Onboarding");

        group.MapPost("/register", async (RegisterTenantRequest req, WorkBaseDbContext db) =>
        {
            var existing = await db.Set<OnboardingRequest>().AnyAsync(o => o.AdminEmail == req.AdminEmail && o.Status != "failed");
            if (existing) return Results.Conflict(new { Error = "Konto z tym adresem email już istnieje lub jest w trakcie rejestracji." });

            var request = new OnboardingRequest
            {
                Id = Guid.NewGuid(), CompanyName = req.CompanyName,
                AdminEmail = req.AdminEmail, AdminFullName = req.AdminFullName,
                Phone = req.Phone, PlanId = req.PlanId,
                Status = "pending", CreatedAt = DateTime.UtcNow
            };
            db.Set<OnboardingRequest>().Add(request);
            await db.SaveChangesAsync();
            return Results.Accepted($"/api/onboarding/{request.Id}", new { RequestId = request.Id, Status = "pending" });
        }).WithName("RegisterTenant").WithSummary("Rejestracja nowego tenanta (self-service)").AllowAnonymous();

        group.MapGet("/{id:guid}/status", async (Guid id, WorkBaseDbContext db) =>
        {
            var req = await db.Set<OnboardingRequest>().FindAsync(id);
            if (req is null) return Results.NotFound();
            return Results.Ok(new { req.Id, req.Status, req.TenantId, req.CompletedAt, req.ErrorMessage });
        }).WithName("GetOnboardingStatus").WithSummary("Sprawdź status rejestracji").AllowAnonymous();

        return endpoints;
    }
}

public static class BillingEndpoints
{
    public static IEndpointRouteBuilder MapBillingEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/billing").WithTags("Billing").RequireAuthorization();

        group.MapGet("/subscription", async (WorkBaseDbContext db, HttpContext http) =>
        {
            var tenantId = http.User.GetTenantId();
            if (tenantId is null) return Results.Forbid();
            var sub = await db.Set<BillingSubscription>().FirstOrDefaultAsync(s => s.TenantId == tenantId.Value);
            return sub is not null ? Results.Ok(sub) : Results.NotFound();
        }).WithName("GetSubscription").WithSummary("Pobierz aktualną subskrypcję");

        group.MapGet("/invoices", async (WorkBaseDbContext db, HttpContext http) =>
        {
            var tenantId = http.User.GetTenantId();
            if (tenantId is null) return Results.Forbid();
            var invoices = await db.Set<BillingInvoice>()
                .Where(i => i.TenantId == tenantId.Value)
                .OrderByDescending(i => i.CreatedAt).Take(50).ToListAsync();
            return Results.Ok(invoices);
        }).WithName("GetInvoices").WithSummary("Pobierz faktury");

        // Stripe webhook — signature verified against Stripe:WebhookSecret before any processing
        endpoints.MapPost("/api/billing/webhook", async (HttpContext http, WorkBaseDbContext db, IConfiguration configuration, ILogger<Program> logger) =>
        {
            var webhookSecret = configuration["Stripe:WebhookSecret"];
            if (string.IsNullOrEmpty(webhookSecret))
            {
                logger.LogError("Stripe webhook received but Stripe:WebhookSecret is not configured; rejecting request.");
                return Results.Problem(statusCode: StatusCodes.Status503ServiceUnavailable, title: "Webhook not configured");
            }

            var json = await new StreamReader(http.Request.Body).ReadToEndAsync();
            var signatureHeader = http.Request.Headers["Stripe-Signature"].ToString();

            Event stripeEvent;
            try
            {
                stripeEvent = EventUtility.ConstructEvent(json, signatureHeader, webhookSecret);
            }
            catch (StripeException ex)
            {
                logger.LogWarning(ex, "Stripe webhook signature verification failed.");
                return Results.BadRequest(new { Error = "Invalid signature" });
            }

            switch (stripeEvent.Type)
            {
                case "customer.subscription.updated":
                case "customer.subscription.created":
                    if (stripeEvent.Data.Object is Subscription updatedSub)
                    {
                        var sub = await db.Set<BillingSubscription>()
                            .FirstOrDefaultAsync(s => s.StripeSubscriptionId == updatedSub.Id);
                        if (sub is not null)
                        {
                            sub.Status = updatedSub.Status;
                            sub.CurrentPeriodStart = updatedSub.CurrentPeriodStart;
                            sub.CurrentPeriodEnd = updatedSub.CurrentPeriodEnd;
                            await db.SaveChangesAsync();
                        }
                    }
                    break;

                case "customer.subscription.deleted":
                    if (stripeEvent.Data.Object is Subscription deletedSub)
                    {
                        var sub = await db.Set<BillingSubscription>()
                            .FirstOrDefaultAsync(s => s.StripeSubscriptionId == deletedSub.Id);
                        if (sub is not null)
                        {
                            sub.Status = "canceled";
                            sub.CanceledAt = DateTime.UtcNow;
                            await db.SaveChangesAsync();
                        }
                    }
                    break;

                case "invoice.paid":
                case "invoice.payment_failed":
                    if (stripeEvent.Data.Object is Invoice invoice)
                    {
                        var billingInvoice = await db.Set<BillingInvoice>()
                            .FirstOrDefaultAsync(i => i.StripeInvoiceId == invoice.Id);
                        if (billingInvoice is null)
                        {
                            var sub = await db.Set<BillingSubscription>()
                                .FirstOrDefaultAsync(s => s.StripeCustomerId == invoice.CustomerId);
                            if (sub is not null)
                            {
                                billingInvoice = new BillingInvoice { Id = Guid.NewGuid(), TenantId = sub.TenantId, StripeInvoiceId = invoice.Id, CreatedAt = DateTime.UtcNow };
                                db.Set<BillingInvoice>().Add(billingInvoice);
                            }
                        }
                        if (billingInvoice is not null)
                        {
                            billingInvoice.Number = invoice.Number ?? billingInvoice.Number;
                            billingInvoice.AmountDue = invoice.AmountDue / 100m;
                            billingInvoice.AmountPaid = invoice.AmountPaid / 100m;
                            billingInvoice.Currency = invoice.Currency?.ToUpperInvariant() ?? billingInvoice.Currency;
                            billingInvoice.Status = invoice.Status ?? billingInvoice.Status;
                            billingInvoice.PdfUrl = invoice.InvoicePdf;
                            billingInvoice.PeriodStart = invoice.PeriodStart;
                            billingInvoice.PeriodEnd = invoice.PeriodEnd;
                            if (stripeEvent.Type == "invoice.paid") billingInvoice.PaidAt = DateTime.UtcNow;
                            await db.SaveChangesAsync();
                        }
                    }
                    break;

                default:
                    logger.LogInformation("Unhandled Stripe webhook event type: {EventType}", stripeEvent.Type);
                    break;
            }

            return Results.Ok(new { Received = true });
        }).WithName("StripeWebhook").WithSummary("Stripe webhook endpoint").AllowAnonymous();

        return endpoints;
    }
}

public sealed record UpdateBrandingRequest(string? LogoUrl, string? FaviconUrl, string PrimaryColor, string SecondaryColor,
    string? AccentColor, string? AppName, string? CustomDomain, string? CustomCss, string? LoginBackgroundUrl, string? FooterHtml);
public sealed record RegisterTenantRequest(string CompanyName, string AdminEmail, string AdminFullName, string? Phone, string PlanId);

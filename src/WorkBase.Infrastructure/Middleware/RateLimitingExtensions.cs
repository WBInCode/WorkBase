using System.Security.Claims;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace WorkBase.Infrastructure.Middleware;

public static class RateLimitingExtensions
{
    /// <summary>
    /// Limit dla rejestracji samoobslugowej: endpoint dziala bez logowania, wiec globalne
    /// 100/min jest o wiele za hojne — pozwalaloby dopisac do bazy tysiace wierszy na godzine
    /// bez posiadania jakiegokolwiek konta.
    /// </summary>
    public const string OnboardingPolicy = "onboarding";

    public static IServiceCollection AddTenantRateLimiting(
        this IServiceCollection services, IConfiguration configuration)
    {
        var permitLimit = configuration.GetValue("RateLimiting:PermitLimit", 100);
        var windowSeconds = configuration.GetValue("RateLimiting:WindowSeconds", 60);
        var queueLimit = configuration.GetValue("RateLimiting:QueueLimit", 10);

        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

            options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
            {
                var tenantId = context.User?.FindFirstValue("tenant_id");
                var partitionKey = tenantId ?? context.Connection.RemoteIpAddress?.ToString() ?? "anonymous";

                return RateLimitPartition.GetFixedWindowLimiter(partitionKey, _ =>
                    new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = permitLimit,
                        Window = TimeSpan.FromSeconds(windowSeconds),
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                        QueueLimit = queueLimit,
                    });
            });

            options.AddPolicy(OnboardingPolicy, context => RateLimitPartition.GetFixedWindowLimiter(
                context.Connection.RemoteIpAddress?.ToString() ?? "anonymous",
                _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = configuration.GetValue("RateLimiting:OnboardingPermitLimit", 5),
                    Window = TimeSpan.FromHours(1),
                    QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                    QueueLimit = 0,
                }));

            options.OnRejected = async (context, cancellationToken) =>
            {
                context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
                context.HttpContext.Response.ContentType = "application/problem+json";

                await context.HttpContext.Response.WriteAsJsonAsync(new
                {
                    type = "https://tools.ietf.org/html/rfc6585#section-4",
                    title = "Too Many Requests",
                    status = 429,
                    detail = "Rate limit exceeded. Please retry after a short delay.",
                }, cancellationToken);
            };
        });

        return services;
    }

    public static IApplicationBuilder UseTenantRateLimiting(this IApplicationBuilder app)
    {
        return app.UseRateLimiter();
    }
}

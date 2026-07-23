using System.Security.Claims;
using Hangfire.Dashboard;
using Microsoft.Extensions.DependencyInjection;
using WorkBase.Shared.Auth;

namespace WorkBase.Infrastructure.BackgroundJobs;

/// <summary>
/// Allows Hangfire Dashboard access only to the platform operator tenant with
/// the database-backed platform.manage-tenants permission. Realm roles are not
/// trusted here because an already-issued token can contain a stale admin role.
/// </summary>
public sealed class HangfireAdminAuthorizationFilter : IDashboardAuthorizationFilter
{
    public bool Authorize(DashboardContext context)
    {
        var httpContext = context.GetHttpContext();
        var user = httpContext.User;

        if (user.Identity is not { IsAuthenticated: true })
            return false;

        var subject = user.FindFirstValue("sub") ?? user.FindFirstValue(ClaimTypes.NameIdentifier);
        var tenantClaim = user.FindFirstValue("tenant_id");
        if (!Guid.TryParse(subject, out var userId)
            || !Guid.TryParse(tenantClaim, out var tenantId)
            || tenantId != PlatformConstants.OperatorTenantId)
        {
            return false;
        }

        var permissionService = httpContext.RequestServices.GetRequiredService<IPermissionService>();
        return permissionService.HasPermissionAsync(
                userId,
                tenantId,
                PlatformConstants.ManageTenantsPermission,
                httpContext.RequestAborted)
            .GetAwaiter()
            .GetResult();
    }
}

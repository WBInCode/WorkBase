using System.Security.Claims;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace WorkBase.Shared.Auth;

public static class PlatformOperatorEndpointExtensions
{
    /// <summary>
    /// Requires the caller to be a "platform operator": authenticated in our own operator
    /// tenant (<see cref="PlatformConstants.OperatorTenantId"/>) AND holding the
    /// <see cref="PlatformConstants.ManageTenantsPermission"/> permission there.
    ///
    /// This is a deliberately narrow, double-checked gate for cross-tenant endpoints (listing
    /// all companies, applying license plans / toggling feature flags for ANY tenant). Relying
    /// on the permission alone would not be safe: the seeding pattern grants "Super Admin"/"Admin"
    /// roles ALL permissions per-tenant, so a future customer tenant's own Super Admin could
    /// otherwise end up with this permission too. Checking tenant identity as well ensures only
    /// our own operator company can use these endpoints, until true multi-realm platform-admin
    /// accounts exist (docs/05-module-licensing-architecture.md step 6).
    /// </summary>
    public static RouteHandlerBuilder RequirePlatformOperator(this RouteHandlerBuilder builder)
    {
        return builder.AddEndpointFilter(new PlatformOperatorEndpointFilter());
    }
}

internal sealed class PlatformOperatorEndpointFilter : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(
        EndpointFilterInvocationContext context,
        EndpointFilterDelegate next)
    {
        var httpContext = context.HttpContext;
        var user = httpContext.User;

        if (user.Identity?.IsAuthenticated != true)
            return Results.Unauthorized();

        var userId = GetUserId(user);
        var tenantId = GetTenantId(user);

        if (userId is null || tenantId is null || tenantId != PlatformConstants.OperatorTenantId)
        {
            var logger = httpContext.RequestServices.GetRequiredService<ILogger<PlatformOperatorEndpointFilter>>();
            logger.LogWarning(
                "Platform operator access denied: user {UserId} not in operator tenant (actual tenant {TenantId})",
                userId, tenantId);
            return Results.Problem(statusCode: StatusCodes.Status403Forbidden, title: "Forbidden");
        }

        var permissionService = httpContext.RequestServices.GetRequiredService<IPermissionService>();
        var hasPermission = await permissionService.HasPermissionAsync(
            userId.Value, tenantId.Value, PlatformConstants.ManageTenantsPermission, httpContext.RequestAborted);

        if (!hasPermission)
        {
            return Results.Problem(
                statusCode: StatusCodes.Status403Forbidden,
                title: "Forbidden",
                detail: $"Missing required permission: {PlatformConstants.ManageTenantsPermission}");
        }

        return await next(context);
    }

    private static Guid? GetUserId(ClaimsPrincipal user)
    {
        var sub = user.FindFirstValue("sub") ?? user.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(sub, out var id) ? id : null;
    }

    private static Guid? GetTenantId(ClaimsPrincipal user)
    {
        var tenant = user.FindFirstValue("tenant_id");
        return Guid.TryParse(tenant, out var id) ? id : null;
    }
}

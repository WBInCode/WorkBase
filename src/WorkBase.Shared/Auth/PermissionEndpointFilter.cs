using System.Security.Claims;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace WorkBase.Shared.Auth;

public static class PermissionEndpointExtensions
{
    /// <summary>
    /// Requires the specified permission(s) to access this endpoint.
    /// Usage: group.MapGet("/", Handler).RequirePermission("org.view");
    /// </summary>
    public static RouteHandlerBuilder RequirePermission(this RouteHandlerBuilder builder, params string[] permissions)
    {
        return builder.AddEndpointFilter(new PermissionEndpointFilter(permissions));
    }
}

internal sealed class PermissionEndpointFilter(string[] permissions) : IEndpointFilter
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

        if (userId is null || tenantId is null)
            return Results.Forbid();

        var permissionService = httpContext.RequestServices.GetRequiredService<IPermissionService>();
        var logger = httpContext.RequestServices.GetRequiredService<ILogger<PermissionEndpointFilter>>();

        var userPermissions = await permissionService.GetUserPermissionsAsync(
            userId.Value, tenantId.Value, httpContext.RequestAborted);

        foreach (var required in permissions)
        {
            if (!userPermissions.Contains(required))
            {
                logger.LogWarning(
                    "Permission denied: User {UserId} lacks '{Permission}' in tenant {TenantId}",
                    userId.Value, required, tenantId.Value);

                return Results.Problem(
                    statusCode: StatusCodes.Status403Forbidden,
                    title: "Forbidden",
                    detail: $"Missing required permission: {required}");
            }
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

using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;
using WorkBase.Shared.Auth;

namespace WorkBase.Infrastructure.Auth;

public sealed class PermissionRequirement(string permission) : IAuthorizationRequirement
{
    public string Permission { get; } = permission;
}

public sealed class PermissionAuthorizationHandler(
    IPermissionService permissionService,
    ILogger<PermissionAuthorizationHandler> logger) : AuthorizationHandler<PermissionRequirement>
{
    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        PermissionRequirement requirement)
    {
        var userId = GetUserId(context.User);
        var tenantId = GetTenantId(context.User);

        if (userId is null || tenantId is null)
        {
            logger.LogWarning("Permission check failed: missing userId or tenantId claims");
            return;
        }

        var hasPermission = await permissionService.HasPermissionAsync(
            userId.Value,
            tenantId.Value,
            requirement.Permission);

        if (hasPermission)
        {
            context.Succeed(requirement);
        }
        else
        {
            logger.LogWarning(
                "Permission denied: User {UserId} lacks permission '{Permission}' in tenant {TenantId}",
                userId.Value, requirement.Permission, tenantId.Value);
        }
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

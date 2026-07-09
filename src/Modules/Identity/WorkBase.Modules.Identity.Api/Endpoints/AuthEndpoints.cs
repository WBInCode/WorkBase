using System.Security.Claims;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;
using WorkBase.Shared.Auth;

namespace WorkBase.Modules.Identity.Api.Endpoints;

public static class AuthEndpoints
{
    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/auth")
            .WithTags("Auth")
            .RequireAuthorization();

        group.MapGet("/me", GetCurrentUser)
            .WithName("GetCurrentUser")
            .WithSummary("Zwraca kontekst zalogowanego użytkownika")
            .Produces<CurrentUserResponse>()
            .Produces(StatusCodes.Status401Unauthorized);

        return endpoints;
    }

    private static async Task<IResult> GetCurrentUser(
        ClaimsPrincipal user, IPermissionService permissionService, IRoleManagementService roleService,
        ILogger<CurrentUserResponse> logger)
    {
        if (user.Identity?.IsAuthenticated != true)
            return Results.Unauthorized();

        var sub = user.FindFirstValue("sub") ?? "";
        var tenantIdClaim = user.FindFirstValue("tenant_id") ?? "";
        var keycloakRoles = user.FindAll("roles").Select(c => c.Value).ToArray();

        string[] permissions = [];
        // "Admin" for UI purposes is primarily derived from the app's own Role/UserRole data
        // (the same source of truth RequirePermission checks against), NOT the Keycloak
        // "roles" claim — assigning a System-type role via the in-app Roles screen has zero
        // effect on Keycloak. BUT: some accounts (e.g. pre-existing ones provisioned before
        // this consistency fix, or created directly in Keycloak without a matching UserRole
        // row) may have a Keycloak admin role with no corresponding DB UserRole — without a
        // fallback those users would suddenly lose all admin UI access. So: DB System-role
        // check first (authoritative), falling back to the legacy Keycloak claim check only
        // if that finds nothing. See docs/AUDIT-KNOWLEDGE-MAP.md (role system consistency).
        var isAdmin = false;
        if (Guid.TryParse(sub, out var userId) && Guid.TryParse(tenantIdClaim, out var tenantId))
        {
            try
            {
                var userPermissions = await permissionService.GetUserPermissionsAsync(userId, tenantId);
                permissions = [.. userPermissions];

                var userRoles = await roleService.GetUserRolesAsync(userId, tenantId);
                isAdmin = userRoles.Any(r => r.RoleType == "System");
            }
            catch (Exception ex)
            {
                // Never let a DB/permission lookup failure 500 this endpoint — it underpins
                // basic navigation for every authenticated user. Falls through to the
                // Keycloak-claim fallback below.
                logger.LogWarning(ex,
                    "Failed to load app-level roles/permissions for user {UserId} in tenant {TenantId}; falling back to Keycloak claim for admin check.",
                    userId, tenantId);
            }
        }

        if (!isAdmin)
        {
            isAdmin = keycloakRoles.Any(r => r is "workbase-admin" or "Admin" or "Super Admin");
        }

        var response = new CurrentUserResponse
        {
            UserId = sub,
            Email = user.FindFirstValue("email") ?? "",
            Name = user.FindFirstValue("name")
                ?? user.FindFirstValue("preferred_username")
                ?? "",
            TenantId = tenantIdClaim,
            EmployeeId = user.FindFirstValue("employee_id") ?? "",
            Roles = keycloakRoles,
            Permissions = permissions,
            IsAdmin = isAdmin,
            OrgUnitIds = [],
            ScopeLevel = "self"
        };

        return Results.Ok(response);
    }
}

public sealed class CurrentUserResponse
{
    public required string UserId { get; init; }
    public required string Email { get; init; }
    public required string Name { get; init; }
    public required string TenantId { get; init; }
    public required string EmployeeId { get; init; }
    public required string[] Roles { get; init; }
    public required string[] Permissions { get; init; }
    public required bool IsAdmin { get; init; }
    public required string[] OrgUnitIds { get; init; }
    public required string ScopeLevel { get; init; }
}

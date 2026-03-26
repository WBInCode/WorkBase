using System.Security.Claims;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
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

    private static async Task<IResult> GetCurrentUser(ClaimsPrincipal user, IPermissionService permissionService)
    {
        if (user.Identity?.IsAuthenticated != true)
            return Results.Unauthorized();

        var sub = user.FindFirstValue("sub") ?? "";
        var tenantIdClaim = user.FindFirstValue("tenant_id") ?? "";

        string[] permissions = [];
        if (Guid.TryParse(sub, out var userId) && Guid.TryParse(tenantIdClaim, out var tenantId))
        {
            var userPermissions = await permissionService.GetUserPermissionsAsync(userId, tenantId);
            permissions = [.. userPermissions];
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
            Roles = user.FindAll("roles").Select(c => c.Value).ToArray(),
            Permissions = permissions,
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
    public required string[] OrgUnitIds { get; init; }
    public required string ScopeLevel { get; init; }
}

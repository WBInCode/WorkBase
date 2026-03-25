using System.Security.Claims;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

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

    private static IResult GetCurrentUser(ClaimsPrincipal user)
    {
        if (user.Identity?.IsAuthenticated != true)
            return Results.Unauthorized();

        var response = new CurrentUserResponse
        {
            UserId = user.FindFirstValue("sub") ?? "",
            Email = user.FindFirstValue("email") ?? "",
            Name = user.FindFirstValue("name")
                ?? user.FindFirstValue("preferred_username")
                ?? "",
            TenantId = user.FindFirstValue("tenant_id") ?? "",
            EmployeeId = user.FindFirstValue("employee_id") ?? "",
            Roles = user.FindAll("roles").Select(c => c.Value).ToArray(),
            // Populated after E06 (RBAC) implementation
            Permissions = [],
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

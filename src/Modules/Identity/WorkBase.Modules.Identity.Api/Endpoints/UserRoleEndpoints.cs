using System.Security.Claims;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using WorkBase.Shared.Auth;

namespace WorkBase.Modules.Identity.Api.Endpoints;

public static class UserRoleEndpoints
{
    public static IEndpointRouteBuilder MapUserRoleEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/iam/users")
            .WithTags("IAM – User Roles")
            .RequireAuthorization();

        group.MapGet("/{userId:guid}/roles", GetUserRoles)
            .WithName("GetUserRoles")
            .WithSummary("Pobierz role użytkownika")
            .RequirePermission("identity.view")
            .Produces<IReadOnlyList<UserRoleDto>>();

        group.MapPost("/{userId:guid}/roles", AssignUserRole)
            .WithName("AssignUserRole")
            .WithSummary("Przypisz rolę do użytkownika")
            .RequirePermission("identity.assign-roles")
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status400BadRequest);

        group.MapDelete("/{userId:guid}/roles/{roleId:guid}", UnassignUserRole)
            .WithName("UnassignUserRole")
            .WithSummary("Usuń rolę z użytkownika")
            .RequirePermission("identity.assign-roles")
            .Produces(StatusCodes.Status204NoContent);

        return endpoints;
    }

    private static async Task<IResult> GetUserRoles(
        Guid userId,
        ClaimsPrincipal user,
        IRoleManagementService service,
        CancellationToken ct)
    {
        var tenantId = GetTenantId(user);
        if (tenantId is null) return Results.Forbid();

        var roles = await service.GetUserRolesAsync(userId, tenantId.Value, ct);
        return Results.Ok(roles);
    }

    private static async Task<IResult> AssignUserRole(
        Guid userId,
        AssignUserRoleRequest request,
        ClaimsPrincipal user,
        IRoleManagementService service,
        CancellationToken ct)
    {
        var tenantId = GetTenantId(user);
        if (tenantId is null) return Results.Forbid();

        var assignedBy = user.FindFirstValue("sub");
        await service.AssignUserRoleAsync(userId, request.RoleId, tenantId.Value, assignedBy, ct);
        return Results.NoContent();
    }

    private static async Task<IResult> UnassignUserRole(
        Guid userId,
        Guid roleId,
        ClaimsPrincipal user,
        IRoleManagementService service,
        CancellationToken ct)
    {
        var tenantId = GetTenantId(user);
        if (tenantId is null) return Results.Forbid();

        await service.UnassignUserRoleAsync(userId, roleId, tenantId.Value, ct);
        return Results.NoContent();
    }

    private static Guid? GetTenantId(ClaimsPrincipal user)
    {
        var claim = user.FindFirstValue("tenant_id");
        return Guid.TryParse(claim, out var id) ? id : null;
    }
}

public sealed record AssignUserRoleRequest(Guid RoleId);

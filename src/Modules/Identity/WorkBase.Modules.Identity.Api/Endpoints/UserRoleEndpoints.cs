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
        if (!await IsCompanyAdminAsync(user, service, tenantId.Value, ct)) return Results.Forbid();

        var assignedBy = user.FindFirstValue("sub");
        try
        {
            await service.AssignUserRoleAsync(userId, request.RoleId, tenantId.Value, assignedBy, ct);
            return Results.NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return Results.BadRequest(new { ex.Message });
        }
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
        if (!await IsCompanyAdminAsync(user, service, tenantId.Value, ct)) return Results.Forbid();

        try
        {
            await service.UnassignUserRoleAsync(userId, roleId, tenantId.Value, ct);
            return Results.NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return Results.BadRequest(new { ex.Message });
        }
    }

    /// <summary>
    /// Nadawanie i odbieranie ról jest zarezerwowane dla ról systemowych firmy (Admin, Super Admin),
    /// nawet jeśli inna rola dostanie uprawnienie identity.assign-roles w macierzy uprawnień.
    /// </summary>
    private static async Task<bool> IsCompanyAdminAsync(
        ClaimsPrincipal user,
        IRoleManagementService service,
        Guid tenantId,
        CancellationToken ct)
    {
        if (Guid.TryParse(user.FindFirstValue("sub"), out var callerId))
        {
            var roles = await service.GetUserRolesAsync(callerId, tenantId, ct);
            if (roles.Any(role => role.RoleType == "System")) return true;
        }

        // Konta sprzed synchronizacji ról mają rolę administratora tylko w Keycloaku (jak /api/auth/me).
        return user.FindAll("roles").Any(claim => claim.Value is "workbase-admin" or "Admin" or "Super Admin");
    }

    private static Guid? GetTenantId(ClaimsPrincipal user)
    {
        var claim = user.FindFirstValue("tenant_id");
        return Guid.TryParse(claim, out var id) ? id : null;
    }
}

public sealed record AssignUserRoleRequest(Guid RoleId);

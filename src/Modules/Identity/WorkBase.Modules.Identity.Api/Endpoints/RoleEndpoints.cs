using System.Security.Claims;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using WorkBase.Shared.Auth;

namespace WorkBase.Modules.Identity.Api.Endpoints;

public static class RoleEndpoints
{
    public static IEndpointRouteBuilder MapRoleEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/iam/roles")
            .WithTags("IAM – Roles")
            .RequireAuthorization();

        group.MapGet("/", GetRoles)
            .WithName("GetRoles")
            .WithSummary("Pobierz listę ról")
            .RequirePermission("identity.view")
            .Produces<IReadOnlyList<RoleDto>>();

        group.MapGet("/{id:guid}", GetRoleById)
            .WithName("GetRoleById")
            .WithSummary("Pobierz rolę po ID")
            .RequirePermission("identity.view")
            .Produces<RoleDto>()
            .Produces(StatusCodes.Status404NotFound);

        group.MapPost("/", CreateRole)
            .WithName("CreateRole")
            .WithSummary("Utwórz nową rolę")
            .RequirePermission("identity.create")
            .Produces<Guid>(StatusCodes.Status201Created)
            .Produces(StatusCodes.Status400BadRequest);

        group.MapPut("/{id:guid}", UpdateRole)
            .WithName("UpdateRole")
            .WithSummary("Zaktualizuj rolę")
            .RequirePermission("identity.edit")
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status404NotFound);

        group.MapGet("/{id:guid}/permissions", GetRolePermissions)
            .WithName("GetRolePermissions")
            .WithSummary("Pobierz uprawnienia roli")
            .RequirePermission("identity.view")
            .Produces<IReadOnlyList<Guid>>();

        group.MapPut("/{id:guid}/permissions", UpdateRolePermissions)
            .WithName("UpdateRolePermissions")
            .WithSummary("Ustaw uprawnienia roli")
            .RequirePermission("identity.edit")
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status400BadRequest);

        group.MapDelete("/{id:guid}", DeleteRole)
            .WithName("DeleteRole")
            .WithSummary("Usuń rolę")
            .RequirePermission("identity.edit")
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status400BadRequest);

        return endpoints;
    }

    private static async Task<IResult> GetRoles(
        ClaimsPrincipal user,
        IRoleManagementService service,
        CancellationToken ct)
    {
        var tenantId = GetTenantId(user);
        if (tenantId is null) return Results.Forbid();

        var roles = await service.GetRolesAsync(tenantId.Value, ct);
        return Results.Ok(roles);
    }

    private static async Task<IResult> GetRoleById(
        Guid id,
        IRoleManagementService service,
        CancellationToken ct)
    {
        var role = await service.GetRoleByIdAsync(id, ct);
        return role is not null ? Results.Ok(role) : Results.NotFound();
    }

    private static async Task<IResult> CreateRole(
        CreateRoleRequest request,
        ClaimsPrincipal user,
        IRoleManagementService service,
        CancellationToken ct)
    {
        var tenantId = GetTenantId(user);
        if (tenantId is null) return Results.Forbid();

        if (string.IsNullOrWhiteSpace(request.Name))
            return Results.BadRequest(new { Message = "Nazwa roli jest wymagana." });

        var id = await service.CreateRoleAsync(tenantId.Value, request.Name.Trim(), request.Description?.Trim(), request.Level, ct);
        return Results.Created($"/api/iam/roles/{id}", id);
    }

    private static async Task<IResult> UpdateRole(
        Guid id,
        UpdateRoleRequest request,
        IRoleManagementService service,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            return Results.BadRequest(new { Message = "Nazwa roli jest wymagana." });

        try
        {
            await service.UpdateRoleAsync(id, request.Name.Trim(), request.Description?.Trim(), request.Level, ct);
            return Results.NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return Results.BadRequest(new { ex.Message });
        }
    }

    private static async Task<IResult> GetRolePermissions(
        Guid id,
        IRoleManagementService service,
        CancellationToken ct)
    {
        var permissionIds = await service.GetRolePermissionIdsAsync(id, ct);
        return Results.Ok(permissionIds);
    }

    private static async Task<IResult> UpdateRolePermissions(
        Guid id,
        UpdateRolePermissionsRequest request,
        IRoleManagementService service,
        CancellationToken ct)
    {
        try
        {
            await service.UpdateRolePermissionsAsync(id, request.PermissionIds, ct);
            return Results.NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return Results.BadRequest(new { ex.Message });
        }
    }

    private static async Task<IResult> DeleteRole(
        Guid id,
        IRoleManagementService service,
        CancellationToken ct)
    {
        try
        {
            await service.DeleteRoleAsync(id, ct);
            return Results.NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return Results.BadRequest(new { ex.Message });
        }
    }

    private static Guid? GetTenantId(ClaimsPrincipal user)
    {
        var claim = user.FindFirstValue("tenant_id");
        return Guid.TryParse(claim, out var id) ? id : null;
    }
}

public sealed record CreateRoleRequest(string Name, string? Description, int Level = 0);
public sealed record UpdateRoleRequest(string Name, string? Description, int Level = 0);
public sealed record UpdateRolePermissionsRequest(List<Guid> PermissionIds);

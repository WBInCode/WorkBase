using System.Security.Claims;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using WorkBase.Shared.Auth;

namespace WorkBase.Modules.Identity.Api.Endpoints;

public static class PermissionEndpoints
{
    public static IEndpointRouteBuilder MapPermissionEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/iam/permissions")
            .WithTags("IAM – Permissions")
            .RequireAuthorization();

        group.MapGet("/", GetAllPermissions)
            .WithName("GetAllPermissions")
            .WithSummary("Pobierz listę wszystkich uprawnień")
            .RequirePermission("identity.view")
            .Produces<IReadOnlyList<PermissionDto>>();

        group.MapGet("/matrix", GetPermissionMatrix)
            .WithName("GetPermissionMatrix")
            .WithSummary("Pobierz macierz uprawnień (role × uprawnienia)")
            .RequirePermission("identity.view")
            .Produces<PermissionMatrixDto>();

        return endpoints;
    }

    private static async Task<IResult> GetAllPermissions(
        IRoleManagementService service,
        CancellationToken ct)
    {
        var permissions = await service.GetAllPermissionsAsync(ct);
        return Results.Ok(permissions);
    }

    private static async Task<IResult> GetPermissionMatrix(
        ClaimsPrincipal user,
        IRoleManagementService service,
        CancellationToken ct)
    {
        var tenantClaim = user.FindFirstValue("tenant_id");
        if (!Guid.TryParse(tenantClaim, out var tenantId))
            return Results.Forbid();

        var roles = await service.GetRolesAsync(tenantId, ct);
        var permissions = await service.GetAllPermissionsAsync(ct);

        var matrix = new List<PermissionMatrixRowDto>();
        foreach (var role in roles)
        {
            var rolePermissionIds = await service.GetRolePermissionIdsAsync(role.Id, ct);
            matrix.Add(new PermissionMatrixRowDto(role.Id, role.Name, rolePermissionIds.ToList()));
        }

        return Results.Ok(new PermissionMatrixDto(
            permissions.Select(p => new PermissionMatrixColumnDto(p.Id, p.FullCode, p.Module)).ToList(),
            matrix));
    }
}

public sealed record PermissionMatrixDto(
    List<PermissionMatrixColumnDto> Permissions,
    List<PermissionMatrixRowDto> Roles);

public sealed record PermissionMatrixColumnDto(Guid Id, string Code, string Module);

public sealed record PermissionMatrixRowDto(Guid RoleId, string RoleName, List<Guid> PermissionIds);

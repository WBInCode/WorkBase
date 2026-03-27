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

        return endpoints;
    }

    private static async Task<IResult> GetAllPermissions(
        IRoleManagementService service,
        CancellationToken ct)
    {
        var permissions = await service.GetAllPermissionsAsync(ct);
        return Results.Ok(permissions);
    }
}

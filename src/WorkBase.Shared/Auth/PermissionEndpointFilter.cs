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
        // Kody trafiaja takze do metadanych endpointu, zeby dalo sie je odczytac bez wywolywania
        // zadania HTTP. Korzysta z tego test pilnujacy, ze kazde wymagane uprawnienie istnieje
        // w slowniku — brak wpisu oznacza endpoint niedostepny dla nikogo, lacznie z Super Adminem.
        return builder
            .WithMetadata(new RequiredPermissionsMetadata(permissions))
            .AddEndpointFilter(new PermissionEndpointFilter(permissions));
    }

    /// <summary>
    /// Wymaga co najmniej jednego z podanych uprawnien. Uzywane tam, gdzie do endpointu dochodza
    /// role o roznym zasiegu, a samo zawezenie danych dzieje sie w handlerze.
    /// </summary>
    public static RouteHandlerBuilder RequireAnyPermission(this RouteHandlerBuilder builder, params string[] permissions)
    {
        return builder
            .WithMetadata(new RequiredPermissionsMetadata(permissions))
            .AddEndpointFilter(new AnyPermissionEndpointFilter(permissions));
    }
}

/// <summary>Uprawnienia wymagane przez endpoint, wystawione jako metadane do celow diagnostycznych i testowych.</summary>
public sealed record RequiredPermissionsMetadata(IReadOnlyList<string> Permissions);

internal sealed class AnyPermissionEndpointFilter(string[] permissions) : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(
        EndpointFilterInvocationContext context,
        EndpointFilterDelegate next)
    {
        var httpContext = context.HttpContext;
        var user = httpContext.User;

        if (user.Identity?.IsAuthenticated != true)
            return Results.Unauthorized();

        var userId = PermissionClaims.GetUserId(user);
        var tenantId = PermissionClaims.GetTenantId(user);

        if (userId is null || tenantId is null)
            return Results.Forbid();

        var permissionService = httpContext.RequestServices.GetRequiredService<IPermissionService>();
        var userPermissions = await permissionService.GetUserPermissionsAsync(
            userId.Value, tenantId.Value, httpContext.RequestAborted);

        if (permissions.Any(userPermissions.Contains))
            return await next(context);

        httpContext.RequestServices
            .GetRequiredService<ILogger<AnyPermissionEndpointFilter>>()
            .LogWarning(
                "Permission denied: User {UserId} lacks any of '{Permissions}' in tenant {TenantId}",
                userId.Value, string.Join(", ", permissions), tenantId.Value);

        return Results.Problem(
            statusCode: StatusCodes.Status403Forbidden,
            title: "Forbidden",
            detail: $"Brak wymaganego uprawnienia: jednego z {string.Join(", ", permissions)}.");
    }
}

/// <summary>Odczyt identyfikatorow z tokenu, wspoldzielony przez filtry uprawnien i endpointy.</summary>
public static class PermissionClaims
{
    public static Guid? GetUserId(ClaimsPrincipal user)
    {
        var sub = user.FindFirstValue("sub") ?? user.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(sub, out var id) ? id : null;
    }

    public static Guid? GetTenantId(ClaimsPrincipal user)
    {
        var tenant = user.FindFirstValue("tenant_id");
        return Guid.TryParse(tenant, out var id) ? id : null;
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
                    detail: $"Brak wymaganego uprawnienia: {required}.");
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

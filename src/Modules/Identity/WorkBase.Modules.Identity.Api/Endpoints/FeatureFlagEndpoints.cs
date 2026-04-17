using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using System.Security.Claims;
using WorkBase.Modules.Identity.Application.Contracts;
using WorkBase.Shared.Auth;

namespace WorkBase.Modules.Identity.Api.Endpoints;

public static class FeatureFlagEndpoints
{
    public static IEndpointRouteBuilder MapFeatureFlagEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/iam/feature-flags")
            .WithTags("IAM – Feature Flags")
            .RequireAuthorization();

        group.MapGet("/", GetFeatureFlags)
            .WithName("GetFeatureFlags")
            .WithSummary("Pobierz flagi funkcjonalności")
            .RequirePermission("identity.view")
            .Produces<List<FeatureFlagDto>>();

        group.MapPut("/{module}/toggle", ToggleFeatureFlag)
            .WithName("ToggleFeatureFlag")
            .WithSummary("Przełącz flagę funkcjonalności modułu")
            .RequirePermission("identity.edit")
            .Produces(StatusCodes.Status204NoContent);

        return endpoints;
    }

    private static async Task<IResult> GetFeatureFlags(
        ClaimsPrincipal user, IFeatureFlagService service, CancellationToken ct)
    {
        var tenantId = GetTenantId(user);
        if (tenantId is null) return Results.Forbid();

        var flags = await service.GetByTenantAsync(tenantId.Value, ct);
        var dtos = flags.Select(f => new FeatureFlagDto(f.Module, f.IsEnabled, f.EnabledAt, f.EnabledBy)).ToList();
        return Results.Ok(dtos);
    }

    private static async Task<IResult> ToggleFeatureFlag(
        string module, ClaimsPrincipal user,
        IFeatureFlagService service, CancellationToken ct)
    {
        var tenantId = GetTenantId(user);
        if (tenantId is null) return Results.Forbid();

        var userId = user.FindFirstValue("sub");
        await service.ToggleAsync(tenantId.Value, module, userId, ct);
        return Results.NoContent();
    }

    private static Guid? GetTenantId(ClaimsPrincipal user)
    {
        var claim = user.FindFirstValue("tenant_id");
        return Guid.TryParse(claim, out var id) ? id : null;
    }
}

public sealed record FeatureFlagDto(string Module, bool IsEnabled, DateTime? EnabledAt, string? EnabledBy);

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using System.Security.Claims;
using WorkBase.Modules.Identity.Application.Contracts;
using WorkBase.Shared.Api;
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

        group.MapPost("/apply-plan/{planId:guid}", ApplyLicensePlan)
            .WithName("ApplyLicensePlan")
            .WithSummary("Zastosuj pakiet licencyjny (Bronze/Silver/Gold) dla własnego tenanta")
            .RequirePermission("identity.edit")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapGet("/plans", GetLicensePlans)
            .WithName("GetLicensePlans")
            .WithSummary("Pobierz listę dostępnych pakietów licencyjnych (Bronze/Silver/Gold)")
            .RequirePermission("identity.view")
            .Produces<List<LicensePlanSummaryDto>>();

        // --- Platform-operator endpoints: act on an ARBITRARY tenant, not just the caller's own.
        // Gated by RequirePlatformOperator() instead of RequirePermission() (see docs/05-module-licensing-architecture.md step 5).
        group.MapGet("/tenant/{tenantId:guid}", GetFeatureFlagsForTenant)
            .WithName("GetFeatureFlagsForTenant")
            .WithSummary("[Operator] Pobierz flagi funkcjonalności dowolnego tenanta")
            .RequirePlatformOperator()
            .Produces<List<FeatureFlagDto>>();

        group.MapPut("/tenant/{tenantId:guid}/{module}/toggle", ToggleFeatureFlagForTenant)
            .WithName("ToggleFeatureFlagForTenant")
            .WithSummary("[Operator] Przełącz flagę funkcjonalności modułu dla dowolnego tenanta")
            .RequirePlatformOperator()
            .Produces(StatusCodes.Status204NoContent);

        group.MapPost("/tenant/{tenantId:guid}/apply-plan/{planId:guid}", ApplyLicensePlanForTenant)
            .WithName("ApplyLicensePlanForTenant")
            .WithSummary("[Operator] Zastosuj pakiet licencyjny dla dowolnego tenanta")
            .RequirePlatformOperator()
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status404NotFound);

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

    /// <summary>
    /// Applies a LicensePlan (e.g. Bronze/Silver/Gold) to the caller's own tenant, rewriting
    /// its FeatureFlag rows to match the plan's IncludedModules. Scoped to the caller's own
    /// tenant_id claim only — there is no cross-tenant "platform operator" endpoint yet
    /// (planned as part of docs/05-module-licensing-architecture.md step 5/6); exposing an
    /// arbitrary tenantId here today would let any tenant admin change another company's
    /// licensing, so this intentionally always targets the caller's own tenant.
    /// </summary>
    private static async Task<IResult> ApplyLicensePlan(
        Guid planId, ClaimsPrincipal user,
        IFeatureFlagService service, CancellationToken ct)
    {
        var tenantId = GetTenantId(user);
        if (tenantId is null) return Results.Forbid();

        var userId = user.FindFirstValue("sub");
        var result = await service.ApplyPlanAsync(tenantId.Value, planId, userId, ct);
        return result.ToHttpResult();
    }

    private static async Task<IResult> GetLicensePlans(IFeatureFlagService service, CancellationToken ct)
    {
        var plans = await service.GetActivePlansAsync(ct);
        return Results.Ok(plans);
    }

    private static async Task<IResult> GetFeatureFlagsForTenant(
        Guid tenantId, IFeatureFlagService service, CancellationToken ct)
    {
        var flags = await service.GetByTenantAsync(tenantId, ct);
        var dtos = flags.Select(f => new FeatureFlagDto(f.Module, f.IsEnabled, f.EnabledAt, f.EnabledBy)).ToList();
        return Results.Ok(dtos);
    }

    private static async Task<IResult> ToggleFeatureFlagForTenant(
        Guid tenantId, string module, ClaimsPrincipal user,
        IFeatureFlagService service, CancellationToken ct)
    {
        var userId = user.FindFirstValue("sub");
        await service.ToggleAsync(tenantId, module, userId, ct);
        return Results.NoContent();
    }

    private static async Task<IResult> ApplyLicensePlanForTenant(
        Guid tenantId, Guid planId, ClaimsPrincipal user,
        IFeatureFlagService service, CancellationToken ct)
    {
        var userId = user.FindFirstValue("sub");
        var result = await service.ApplyPlanAsync(tenantId, planId, userId, ct);
        return result.ToHttpResult();
    }

    private static Guid? GetTenantId(ClaimsPrincipal user)
    {
        var claim = user.FindFirstValue("tenant_id");
        return Guid.TryParse(claim, out var id) ? id : null;
    }
}

public sealed record FeatureFlagDto(string Module, bool IsEnabled, DateTime? EnabledAt, string? EnabledBy);

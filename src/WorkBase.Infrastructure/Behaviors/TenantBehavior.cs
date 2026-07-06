using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using WorkBase.Infrastructure.Persistence;
using WorkBase.Modules.Identity.Domain.Entities;
using WorkBase.Shared.Cqrs;
using WorkBase.Shared.Domain;
using WorkBase.Shared.Modules;

namespace WorkBase.Infrastructure.Behaviors;

public sealed class TenantBehavior<TRequest, TResponse>(
    IHttpContextAccessor httpContextAccessor,
    WorkBaseDbContext dbContext)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
    where TResponse : Result
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        if (request is not ITenantRequest tenantRequest)
            return await next();

        var tenantClaim = httpContextAccessor.HttpContext?.User.FindFirstValue("tenant_id");

        if (!Guid.TryParse(tenantClaim, out var tenantId))
        {
            var error = new Error("Tenant.Missing", "Tenant context is required.", ErrorType.Forbidden);
            return CreateFailureResult(error);
        }

        tenantRequest.TenantId = tenantId;

        var moduleDisabledError = await CheckModuleEnabledAsync(tenantId, cancellationToken);
        if (moduleDisabledError is not null)
            return CreateFailureResult(moduleDisabledError);

        return await next();
    }

    /// <summary>
    /// Backend enforcement counterpart to the frontend's FeatureGate/FeatureFlagsPage:
    /// blocks execution of requests belonging to a module that has been explicitly
    /// disabled for the current tenant. See docs/05-module-licensing-architecture.md §4.
    ///
    /// Fail-open by design: if no FeatureFlag row exists yet for (tenant, module) — e.g. a
    /// tenant provisioned before this module existed, or before a LicensePlan was applied —
    /// the module is treated as allowed. Only an explicit IsEnabled = false row blocks access.
    /// This avoids silently breaking modules that were working before this check existed.
    /// </summary>
    private async Task<Error?> CheckModuleEnabledAsync(Guid tenantId, CancellationToken cancellationToken)
    {
        var moduleKey = ModuleResolver.ResolveModuleKey(typeof(TRequest));
        if (moduleKey is null)
            return null;

        var isEnabled = await dbContext.Set<FeatureFlag>()
            .AsNoTracking()
            .Where(f => f.TenantId == tenantId && f.Module == moduleKey)
            .Select(f => (bool?)f.IsEnabled)
            .FirstOrDefaultAsync(cancellationToken);

        if (isEnabled == false)
        {
            return new Error(
                "Module.Disabled",
                $"Moduł '{moduleKey}' nie jest dostępny w Twoim planie.",
                ErrorType.Forbidden);
        }

        return null;
    }

    private static TResponse CreateFailureResult(Error error)
    {
        if (typeof(TResponse) == typeof(Result))
            return (TResponse)Result.Failure(error);

        var resultType = typeof(TResponse).GetGenericArguments()[0];
        var method = typeof(Result)
            .GetMethod(nameof(Result.Failure), 1, [typeof(Error)])!
            .MakeGenericMethod(resultType);

        return (TResponse)method.Invoke(null, [error])!;
    }
}

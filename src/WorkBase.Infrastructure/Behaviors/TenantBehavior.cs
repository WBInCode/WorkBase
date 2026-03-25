using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Http;
using WorkBase.Shared.Domain;

namespace WorkBase.Infrastructure.Behaviors;

/// <summary>
/// Marker interface for requests that require tenant context.
/// TenantId will be resolved from the current user's JWT claims.
/// </summary>
public interface ITenantRequest
{
    Guid TenantId { get; set; }
}

public sealed class TenantBehavior<TRequest, TResponse>(
    IHttpContextAccessor httpContextAccessor)
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

        return await next();
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

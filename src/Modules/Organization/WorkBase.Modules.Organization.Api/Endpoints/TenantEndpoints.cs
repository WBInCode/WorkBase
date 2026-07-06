using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using WorkBase.Modules.Organization.Application.Dtos;
using WorkBase.Modules.Organization.Application.Queries.Tenants;
using WorkBase.Shared.Api;
using WorkBase.Shared.Auth;

namespace WorkBase.Modules.Organization.Api.Endpoints;

/// <summary>
/// Platform-operator "companies" panel endpoints (docs/05-module-licensing-architecture.md
/// step 5). Cross-tenant by design — gated by RequirePlatformOperator(), not RequirePermission().
/// </summary>
public static class TenantEndpoints
{
    public static IEndpointRouteBuilder MapTenantEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/org/tenants")
            .WithTags("Platform – Tenants")
            .RequireAuthorization();

        group.MapGet("/", GetTenants)
            .WithName("GetTenants")
            .WithSummary("Lista wszystkich firm (tenantów) w systemie — tylko dla operatora platformy")
            .RequirePlatformOperator()
            .Produces<List<TenantSummaryDto>>();

        return endpoints;
    }

    private static async Task<IResult> GetTenants(ISender sender, CancellationToken ct)
    {
        var result = await sender.Send(new GetTenantsQuery(), ct);
        return result.ToHttpResult();
    }
}

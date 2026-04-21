using Microsoft.AspNetCore.Routing;
using WorkBase.Modules.Identity.Api.Endpoints;
using WorkBase.Shared.Domain;

namespace WorkBase.Modules.Identity.Api;

public sealed class IdentityEndpointModule : IEndpointModule
{
    public IEndpointRouteBuilder MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        endpoints.MapAuthEndpoints();
        endpoints.MapRoleEndpoints();
        endpoints.MapPermissionEndpoints();
        endpoints.MapUserRoleEndpoints();
        endpoints.MapDataScopeEndpoints();
        endpoints.MapFeatureFlagEndpoints();
        return endpoints;
    }
}

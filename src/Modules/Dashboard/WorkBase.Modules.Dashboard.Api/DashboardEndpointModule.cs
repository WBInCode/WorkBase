using Microsoft.AspNetCore.Routing;
using WorkBase.Modules.Dashboard.Api.Endpoints;
using WorkBase.Shared.Domain;

namespace WorkBase.Modules.Dashboard.Api;

public sealed class DashboardEndpointModule : IEndpointModule
{
    public IEndpointRouteBuilder MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        endpoints.MapDashboardEndpoints();
        return endpoints;
    }
}

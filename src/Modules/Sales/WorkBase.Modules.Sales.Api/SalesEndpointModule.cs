using Microsoft.AspNetCore.Routing;
using WorkBase.Modules.Sales.Api.Endpoints;
using WorkBase.Shared.Domain;

namespace WorkBase.Modules.Sales.Api;

public sealed class SalesEndpointModule : IEndpointModule
{
    public IEndpointRouteBuilder MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        endpoints.MapSalesEndpoints();
        return endpoints;
    }
}

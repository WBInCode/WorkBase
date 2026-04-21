using Microsoft.AspNetCore.Routing;
using WorkBase.Modules.Integration.Api.Endpoints;
using WorkBase.Shared.Domain;

namespace WorkBase.Modules.Integration.Api;

public sealed class IntegrationEndpointModule : IEndpointModule
{
    public IEndpointRouteBuilder MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        endpoints.MapIntegrationEndpoints();
        return endpoints;
    }
}

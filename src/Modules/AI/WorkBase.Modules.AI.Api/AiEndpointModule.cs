using Microsoft.AspNetCore.Routing;
using WorkBase.Modules.AI.Api.Endpoints;
using WorkBase.Shared.Domain;

namespace WorkBase.Modules.AI.Api;

public sealed class AiEndpointModule : IEndpointModule
{
    public IEndpointRouteBuilder MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        endpoints.MapAiEndpoints();
        return endpoints;
    }
}

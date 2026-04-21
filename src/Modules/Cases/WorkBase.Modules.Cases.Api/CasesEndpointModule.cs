using Microsoft.AspNetCore.Routing;
using WorkBase.Modules.Cases.Api.Endpoints;
using WorkBase.Shared.Domain;

namespace WorkBase.Modules.Cases.Api;

public sealed class CasesEndpointModule : IEndpointModule
{
    public IEndpointRouteBuilder MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        endpoints.MapCaseEndpoints();
        return endpoints;
    }
}

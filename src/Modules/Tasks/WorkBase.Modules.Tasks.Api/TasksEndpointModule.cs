using Microsoft.AspNetCore.Routing;
using WorkBase.Modules.Tasks.Api.Endpoints;
using WorkBase.Shared.Domain;

namespace WorkBase.Modules.Tasks.Api;

public sealed class TasksEndpointModule : IEndpointModule
{
    public IEndpointRouteBuilder MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        endpoints.MapTaskEndpoints();
        return endpoints;
    }
}

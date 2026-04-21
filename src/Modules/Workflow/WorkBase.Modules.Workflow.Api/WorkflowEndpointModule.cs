using Microsoft.AspNetCore.Routing;
using WorkBase.Modules.Workflow.Api.Endpoints;
using WorkBase.Shared.Domain;

namespace WorkBase.Modules.Workflow.Api;

public sealed class WorkflowEndpointModule : IEndpointModule
{
    public IEndpointRouteBuilder MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        endpoints.MapWorkflowEndpoints();
        return endpoints;
    }
}

using Microsoft.AspNetCore.Routing;
using WorkBase.Modules.Leave.Api.Endpoints;
using WorkBase.Shared.Domain;

namespace WorkBase.Modules.Leave.Api;

public sealed class LeaveEndpointModule : IEndpointModule
{
    public IEndpointRouteBuilder MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        endpoints.MapLeaveEndpoints();
        return endpoints;
    }
}

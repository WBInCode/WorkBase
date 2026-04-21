using Microsoft.AspNetCore.Routing;
using WorkBase.Modules.Forms.Api.Endpoints;
using WorkBase.Shared.Domain;

namespace WorkBase.Modules.Forms.Api;

public sealed class FormsEndpointModule : IEndpointModule
{
    public IEndpointRouteBuilder MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        endpoints.MapFormsEndpoints();
        return endpoints;
    }
}

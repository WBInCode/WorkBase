using Microsoft.AspNetCore.Routing;
using WorkBase.Modules.Documents.Api.Endpoints;
using WorkBase.Shared.Domain;

namespace WorkBase.Modules.Documents.Api;

public sealed class DocumentsEndpointModule : IEndpointModule
{
    public IEndpointRouteBuilder MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        endpoints.MapDocumentEndpoints();
        return endpoints;
    }
}

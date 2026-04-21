using Microsoft.AspNetCore.Routing;
using WorkBase.Modules.Contacts.Api.Endpoints;
using WorkBase.Shared.Domain;

namespace WorkBase.Modules.Contacts.Api;

public sealed class ContactsEndpointModule : IEndpointModule
{
    public IEndpointRouteBuilder MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        endpoints.MapContactEndpoints();
        return endpoints;
    }
}

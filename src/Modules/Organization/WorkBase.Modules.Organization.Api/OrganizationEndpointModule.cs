using Microsoft.AspNetCore.Routing;
using WorkBase.Modules.Organization.Api.Endpoints;
using WorkBase.Shared.Domain;

namespace WorkBase.Modules.Organization.Api;

public sealed class OrganizationEndpointModule : IEndpointModule
{
    public IEndpointRouteBuilder MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        endpoints.MapOrganizationUnitEndpoints();
        endpoints.MapEmployeeEndpoints();
        endpoints.MapPositionEndpoints();
        endpoints.MapUnitTypeEndpoints();
        return endpoints;
    }
}

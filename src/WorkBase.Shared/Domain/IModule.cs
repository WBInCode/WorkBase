using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace WorkBase.Shared.Domain;

/// <summary>
/// Marker for module service registration auto-discovery.
/// Implemented in each module's Infrastructure layer.
/// </summary>
public interface IModule
{
    /// <summary>Registers module services into DI.</summary>
    IServiceCollection ConfigureServices(IServiceCollection services);
}

/// <summary>
/// Marker for module endpoint auto-discovery.
/// Implemented in each module's Api layer.
/// </summary>
public interface IEndpointModule
{
    /// <summary>Maps module endpoints onto the application.</summary>
    IEndpointRouteBuilder MapEndpoints(IEndpointRouteBuilder endpoints);
}

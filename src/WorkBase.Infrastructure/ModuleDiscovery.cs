using System.Reflection;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using WorkBase.Shared.Domain;

namespace WorkBase.Infrastructure;

public static class ModuleDiscovery
{
    private static readonly string[] ModuleAssemblyNames =
    [
        "WorkBase.Modules.Identity.Infrastructure",
        "WorkBase.Modules.Organization.Infrastructure",
        "WorkBase.Modules.TimeTracking.Infrastructure",
        "WorkBase.Modules.Workflow.Infrastructure",
        "WorkBase.Modules.Leave.Infrastructure",
        "WorkBase.Modules.Tasks.Infrastructure",
        "WorkBase.Modules.Dashboard.Infrastructure",
        "WorkBase.Modules.Notification.Infrastructure",
        "WorkBase.Modules.Documents.Infrastructure",
        "WorkBase.Modules.Integration.Infrastructure",
        "WorkBase.Modules.Identity.Api",
        "WorkBase.Modules.Organization.Api",
        "WorkBase.Modules.TimeTracking.Api",
        "WorkBase.Modules.Workflow.Api",
        "WorkBase.Modules.Leave.Api",
        "WorkBase.Modules.Tasks.Api",
        "WorkBase.Modules.Dashboard.Api",
        "WorkBase.Modules.Notification.Api",
        "WorkBase.Modules.Documents.Api",
        "WorkBase.Modules.Integration.Api"
    ];

    /// <summary>
    /// Scans all module assemblies for IModule implementations and registers their services.
    /// </summary>
    public static IServiceCollection AddModules(this IServiceCollection services)
    {
        foreach (var moduleType in FindImplementations<IModule>())
        {
            var module = (IModule)Activator.CreateInstance(moduleType)!;
            module.ConfigureServices(services);
        }

        return services;
    }

    /// <summary>
    /// Scans all module assemblies for IEndpointModule implementations and maps their endpoints.
    /// </summary>
    public static IEndpointRouteBuilder MapModuleEndpoints(this IEndpointRouteBuilder endpoints)
    {
        foreach (var moduleType in FindImplementations<IEndpointModule>())
        {
            var module = (IEndpointModule)Activator.CreateInstance(moduleType)!;
            module.MapEndpoints(endpoints);
        }

        return endpoints;
    }

    private static IEnumerable<Type> FindImplementations<TInterface>()
    {
        var assemblies = new List<Assembly>();

        // Ensure all module assemblies are loaded
        foreach (var name in ModuleAssemblyNames)
        {
            try { assemblies.Add(Assembly.Load(name)); }
            catch { /* Module not available */ }
        }

        return assemblies
            .SelectMany(a =>
            {
                try { return a.GetTypes(); }
                catch { return []; }
            })
            .Where(t => t is { IsAbstract: false, IsInterface: false }
                        && typeof(TInterface).IsAssignableFrom(t));
    }
}

using System.Reflection;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using WorkBase.Shared.Domain;
using WorkBase.Shared.Modules;

namespace WorkBase.Infrastructure;

public static class ModuleDiscovery
{
    /// <summary>
    /// Infrastructure/Api assembly names for every module, derived from the single
    /// source of truth in <see cref="ModuleCatalog"/>. Adding a module only requires
    /// a new entry in ModuleCatalog.All — no change needed here.
    /// </summary>
    private static readonly string[] ModuleAssemblyNames = ModuleCatalog.All
        .SelectMany(m => new[]
        {
            $"WorkBase.Modules.{m.Namespace}.Infrastructure",
            $"WorkBase.Modules.{m.Namespace}.Api",
        })
        .ToArray();

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

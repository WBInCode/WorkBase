using Microsoft.Extensions.DependencyInjection;
using WorkBase.Modules.Identity.Application.Contracts;
using WorkBase.Modules.Identity.Infrastructure.Services;
using WorkBase.Shared.Domain;

namespace WorkBase.Modules.Identity.Infrastructure;

public sealed class IdentityModule : IModule
{
    public IServiceCollection ConfigureServices(IServiceCollection services)
    {
        services.AddScoped<IDataScopeManagementService, DataScopeManagementService>();
        services.AddScoped<IFeatureFlagService, FeatureFlagService>();
        return services;
    }
}

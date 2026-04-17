using Microsoft.Extensions.DependencyInjection;
using WorkBase.Modules.Dashboard.Application.Contracts;
using WorkBase.Modules.Dashboard.Infrastructure.Queries;

namespace WorkBase.Modules.Dashboard.Infrastructure;

public static class DashboardServiceCollectionExtensions
{
    public static IServiceCollection AddDashboardModule(this IServiceCollection services)
    {
        services.AddScoped<IDashboardQueryService, DapperDashboardQueryService>();
        return services;
    }
}

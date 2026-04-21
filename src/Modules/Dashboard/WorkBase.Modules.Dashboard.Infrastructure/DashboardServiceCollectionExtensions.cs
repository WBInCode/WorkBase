using Microsoft.Extensions.DependencyInjection;
using WorkBase.Modules.Dashboard.Application.Contracts;
using WorkBase.Modules.Dashboard.Infrastructure.Queries;
using WorkBase.Modules.Dashboard.Infrastructure.Repositories;
using WorkBase.Shared.Domain;

namespace WorkBase.Modules.Dashboard.Infrastructure;

public sealed class DashboardModule : IModule
{
    public IServiceCollection ConfigureServices(IServiceCollection services) =>
        services.AddDashboardModule();
}

public static class DashboardServiceCollectionExtensions
{
    public static IServiceCollection AddDashboardModule(this IServiceCollection services)
    {
        services.AddScoped<IDashboardQueryService, DapperDashboardQueryService>();
        services.AddScoped<IDashboardConfigRepository, DashboardConfigRepository>();
        services.AddScoped<IDashboardWidgetRepository, DashboardWidgetRepository>();
        services.AddScoped<IReportDefinitionRepository, ReportDefinitionRepository>();
        return services;
    }
}

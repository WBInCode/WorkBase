using Microsoft.Extensions.DependencyInjection;
using WorkBase.Modules.Sales.Application.Contracts;
using WorkBase.Modules.Sales.Infrastructure.Repositories;
using WorkBase.Shared.Domain;

namespace WorkBase.Modules.Sales.Infrastructure;

public sealed class SalesModule : IModule
{
    public IServiceCollection ConfigureServices(IServiceCollection services) =>
        services.AddSalesModule();
}

public static class SalesServiceCollectionExtensions
{
    public static IServiceCollection AddSalesModule(this IServiceCollection services)
    {
        services.AddScoped<ILeadRepository, LeadRepository>();
        services.AddScoped<IOpportunityRepository, OpportunityRepository>();
        services.AddScoped<IOfferRepository, OfferRepository>();
        services.AddScoped<IPipelineStageRepository, PipelineStageRepository>();
        return services;
    }
}

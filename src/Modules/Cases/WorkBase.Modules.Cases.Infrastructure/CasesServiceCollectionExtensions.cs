using Microsoft.Extensions.DependencyInjection;
using WorkBase.Modules.Cases.Application.Contracts;
using WorkBase.Modules.Cases.Infrastructure.Repositories;
using WorkBase.Shared.Domain;

namespace WorkBase.Modules.Cases.Infrastructure;

public sealed class CasesModule : IModule
{
    public IServiceCollection ConfigureServices(IServiceCollection services) =>
        services.AddCasesModule();
}

public static class CasesServiceCollectionExtensions
{
    public static IServiceCollection AddCasesModule(this IServiceCollection services)
    {
        services.AddScoped<ICaseItemRepository, CaseItemRepository>();
        services.AddScoped<ICaseStatusRepository, CaseStatusRepository>();
        services.AddScoped<ICaseCategoryRepository, CaseCategoryRepository>();
        services.AddScoped<ICaseCommentRepository, CaseCommentRepository>();
        return services;
    }
}

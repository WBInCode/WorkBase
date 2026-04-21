using Microsoft.Extensions.DependencyInjection;
using WorkBase.Modules.AI.Application.Contracts;
using WorkBase.Modules.AI.Infrastructure.Repositories;
using WorkBase.Modules.AI.Infrastructure.Services;
using WorkBase.Shared.Domain;

namespace WorkBase.Modules.AI.Infrastructure;

public sealed class AiModule : IModule
{
    public IServiceCollection ConfigureServices(IServiceCollection services) =>
        services.AddAiModule();
}

public static class AiServiceCollectionExtensions
{
    public static IServiceCollection AddAiModule(this IServiceCollection services)
    {
        services.AddScoped<IAiCompletionService, OpenAiCompletionService>();
        services.AddScoped<IAiEmbeddingService, OpenAiEmbeddingService>();
        services.AddScoped<IAiTaskLogRepository, AiTaskLogRepository>();
        services.AddHttpClient("OpenAI");
        return services;
    }
}

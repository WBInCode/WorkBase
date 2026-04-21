using Microsoft.Extensions.DependencyInjection;
using WorkBase.Modules.Documents.Application.Contracts;
using WorkBase.Modules.Documents.Infrastructure.Persistence;
using WorkBase.Shared.Domain;

namespace WorkBase.Modules.Documents.Infrastructure;

public sealed class DocumentsModule : IModule
{
    public IServiceCollection ConfigureServices(IServiceCollection services) =>
        services.AddDocumentsModule();
}

public static class DocumentsServiceCollectionExtensions
{
    public static IServiceCollection AddDocumentsModule(this IServiceCollection services)
    {
        services.AddScoped<IDocumentRepository, DocumentRepository>();
        services.AddScoped<IDocumentCategoryRepository, DocumentCategoryRepository>();
        return services;
    }
}

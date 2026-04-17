using Microsoft.Extensions.DependencyInjection;
using WorkBase.Modules.Documents.Application.Contracts;
using WorkBase.Modules.Documents.Infrastructure.Persistence;

namespace WorkBase.Modules.Documents.Infrastructure;

public static class DocumentsServiceCollectionExtensions
{
    public static IServiceCollection AddDocumentsModule(this IServiceCollection services)
    {
        services.AddScoped<IDocumentRepository, DocumentRepository>();
        services.AddScoped<IDocumentCategoryRepository, DocumentCategoryRepository>();
        return services;
    }
}

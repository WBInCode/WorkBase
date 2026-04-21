using Microsoft.Extensions.DependencyInjection;
using WorkBase.Modules.Forms.Application.Contracts;
using WorkBase.Modules.Forms.Infrastructure.Repositories;
using WorkBase.Shared.Domain;

namespace WorkBase.Modules.Forms.Infrastructure;

public sealed class FormsModule : IModule
{
    public IServiceCollection ConfigureServices(IServiceCollection services) =>
        services.AddFormsModule();
}

public static class FormsServiceCollectionExtensions
{
    public static IServiceCollection AddFormsModule(this IServiceCollection services)
    {
        services.AddScoped<IFormDefinitionRepository, FormDefinitionRepository>();
        services.AddScoped<IFormFieldRepository, FormFieldRepository>();
        services.AddScoped<IFormSubmissionRepository, FormSubmissionRepository>();
        return services;
    }
}

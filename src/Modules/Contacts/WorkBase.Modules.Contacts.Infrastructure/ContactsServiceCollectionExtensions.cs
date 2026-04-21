using Microsoft.Extensions.DependencyInjection;
using WorkBase.Modules.Contacts.Application.Contracts;
using WorkBase.Modules.Contacts.Infrastructure.Repositories;
using WorkBase.Shared.Domain;

namespace WorkBase.Modules.Contacts.Infrastructure;

public sealed class ContactsModule : IModule
{
    public IServiceCollection ConfigureServices(IServiceCollection services) =>
        services.AddContactsModule();
}

public static class ContactsServiceCollectionExtensions
{
    public static IServiceCollection AddContactsModule(this IServiceCollection services)
    {
        services.AddScoped<IContactRepository, ContactRepository>();
        services.AddScoped<IContactPersonRepository, ContactPersonRepository>();
        return services;
    }
}

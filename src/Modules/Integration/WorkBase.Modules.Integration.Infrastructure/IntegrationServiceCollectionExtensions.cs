using Microsoft.Extensions.DependencyInjection;
using WorkBase.Modules.Integration.Application.Adapters;
using WorkBase.Modules.Integration.Application.Contracts;
using WorkBase.Modules.Integration.Application.Services;
using WorkBase.Modules.Integration.Infrastructure.Adapters;
using WorkBase.Modules.Integration.Infrastructure.Persistence;
using WorkBase.Modules.Integration.Infrastructure.Services;
using WorkBase.Shared.Domain;

namespace WorkBase.Modules.Integration.Infrastructure;

public sealed class IntegrationModule : IModule
{
    public IServiceCollection ConfigureServices(IServiceCollection services) =>
        services.AddIntegrationModule();
}

public static class IntegrationServiceCollectionExtensions
{
    public static IServiceCollection AddIntegrationModule(this IServiceCollection services)
    {
        // Repositories
        services.AddScoped<IIntegrationConnectionRepository, IntegrationConnectionRepository>();
        services.AddScoped<IOAuthTokenRepository, OAuthTokenRepository>();
        services.AddScoped<IWebhookRegistrationRepository, WebhookRegistrationRepository>();

        // Services
        services.AddSingleton<ITokenEncryptionService, AesTokenEncryptionService>();
        services.AddScoped<IOAuthFlowService, OAuthFlowService>();

        // Adapters
        services.AddScoped<GoogleCalendarAdapter>();
        services.AddScoped<Microsoft365CalendarAdapter>();
        services.AddScoped<SlackChatAdapter>();
        services.AddScoped<TeamsChatAdapter>();
        services.AddScoped<GmailEmailAdapter>();

        // Adapter factories
        services.AddScoped<ICalendarAdapterFactory, CalendarAdapterFactory>();
        services.AddScoped<IChatAdapterFactory, ChatAdapterFactory>();
        services.AddScoped<IFileStorageAdapterFactory, FileStorageAdapterFactory>();
        services.AddScoped<IContactSyncAdapterFactory, ContactSyncAdapterFactory>();

        // Deep integration adapters (post-MVP)
        services.AddScoped<GoogleDriveAdapter>();
        services.AddScoped<OneDriveAdapter>();
        services.AddScoped<GoogleContactsAdapter>();
        services.AddScoped<OutlookMailAdapter>();
        services.AddScoped<IFullChatAdapter, SlackFullAdapter>();

        // HttpClients
        services.AddHttpClient("GoogleCalendar");
        services.AddHttpClient("Microsoft365");
        services.AddHttpClient("Slack");
        services.AddHttpClient("Teams");
        services.AddHttpClient("Gmail");
        services.AddHttpClient("OAuth");

        return services;
    }
}

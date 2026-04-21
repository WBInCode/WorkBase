using Microsoft.Extensions.DependencyInjection;
using WorkBase.Contracts;
using WorkBase.Modules.Notification.Application.Contracts;
using WorkBase.Modules.Notification.Infrastructure.Persistence;
using WorkBase.Modules.Notification.Infrastructure.Services;
using WorkBase.Shared.Domain;

namespace WorkBase.Modules.Notification.Infrastructure;

public sealed class NotificationModule : IModule
{
    public IServiceCollection ConfigureServices(IServiceCollection services) =>
        services.AddNotificationModule();
}

public static class NotificationServiceCollectionExtensions
{
    public static IServiceCollection AddNotificationModule(this IServiceCollection services)
    {
        services.AddScoped<INotificationRepository, NotificationRepository>();
        services.AddScoped<INotificationTemplateRepository, NotificationTemplateRepository>();
        services.AddScoped<INotificationPreferenceRepository, NotificationPreferenceRepository>();
        services.AddScoped<IPushSubscriptionRepository, PushSubscriptionRepository>();
        services.AddScoped<INotificationService, NotificationService>();

        return services;
    }
}

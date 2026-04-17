using Microsoft.Extensions.DependencyInjection;
using WorkBase.Contracts;
using WorkBase.Modules.Notification.Application.Contracts;
using WorkBase.Modules.Notification.Infrastructure.Persistence;
using WorkBase.Modules.Notification.Infrastructure.Services;

namespace WorkBase.Modules.Notification.Infrastructure;

public static class NotificationServiceCollectionExtensions
{
    public static IServiceCollection AddNotificationModule(this IServiceCollection services)
    {
        services.AddScoped<INotificationRepository, NotificationRepository>();
        services.AddScoped<INotificationTemplateRepository, NotificationTemplateRepository>();
        services.AddScoped<INotificationPreferenceRepository, NotificationPreferenceRepository>();
        services.AddScoped<INotificationService, NotificationService>();

        return services;
    }
}

using Microsoft.Extensions.DependencyInjection;
using WorkBase.Modules.Integration.Application.Adapters;
using WorkBase.Modules.Integration.Domain.Enums;

namespace WorkBase.Modules.Integration.Infrastructure.Adapters;

internal sealed class CalendarAdapterFactory(IServiceProvider serviceProvider) : ICalendarAdapterFactory
{
    public ICalendarAdapter GetAdapter(IntegrationProvider provider) => provider switch
    {
        IntegrationProvider.GoogleCalendar => serviceProvider.GetRequiredService<GoogleCalendarAdapter>(),
        IntegrationProvider.Microsoft365Calendar => serviceProvider.GetRequiredService<Microsoft365CalendarAdapter>(),
        _ => throw new NotSupportedException($"Calendar adapter not available for provider: {provider}")
    };
}

internal sealed class ChatAdapterFactory(IServiceProvider serviceProvider) : IChatAdapterFactory
{
    public IChatAdapter GetAdapter(IntegrationProvider provider) => provider switch
    {
        IntegrationProvider.Slack => serviceProvider.GetRequiredService<SlackChatAdapter>(),
        IntegrationProvider.MicrosoftTeams => serviceProvider.GetRequiredService<TeamsChatAdapter>(),
        _ => throw new NotSupportedException($"Chat adapter not available for provider: {provider}")
    };
}

internal sealed class FileStorageAdapterFactory(IServiceProvider serviceProvider) : IFileStorageAdapterFactory
{
    public IFileStorageAdapter Create(string provider) => provider.ToLowerInvariant() switch
    {
        "google-drive" or "googledrive" => serviceProvider.GetRequiredService<GoogleDriveAdapter>(),
        "onedrive" or "microsoft-onedrive" => serviceProvider.GetRequiredService<OneDriveAdapter>(),
        _ => throw new NotSupportedException($"File storage adapter not available for: {provider}")
    };
}

internal sealed class ContactSyncAdapterFactory(IServiceProvider serviceProvider) : IContactSyncAdapterFactory
{
    public IContactSyncAdapter Create(string provider) => provider.ToLowerInvariant() switch
    {
        "google-contacts" or "googlecontacts" => serviceProvider.GetRequiredService<GoogleContactsAdapter>(),
        _ => throw new NotSupportedException($"Contact sync adapter not available for: {provider}")
    };
}

using System.Net;
using System.Net.Http.Json;
using Hangfire;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using WorkBase.Contracts;
using WorkBase.Infrastructure.Persistence;
using WorkBase.Modules.Organization.Domain.Entities;

namespace WorkBase.Infrastructure.HubPlatform;

public sealed class HubNotificationForwarder(
    IBackgroundJobClient jobs,
    IConfiguration configuration) : IHubNotificationForwarder
{
    public void Enqueue(Guid tenantId, Guid notificationId)
    {
        var options = configuration.GetSection(HubOptions.SectionName).Get<HubOptions>() ?? new HubOptions();
        if (!options.Enabled
            || string.IsNullOrWhiteSpace(options.BaseUrl)
            || string.IsNullOrWhiteSpace(options.ClientSecret)
            || string.IsNullOrWhiteSpace(options.InstanceId))
            return;

        jobs.Enqueue<HubNotificationJob>(job => job.ExecuteAsync(tenantId, notificationId));
    }
}

/// <summary>
/// Wysyła powiadomienie WorkBase na dzwonek w Hubie. Hub rozpoznaje odbiorcę po adresie e-mail
/// i sam sprawdza, czy ma on dostęp do tej instancji produktu.
/// </summary>
public sealed class HubNotificationJob(
    WorkBaseDbContext dbContext,
    IHttpClientFactory httpClientFactory,
    IConfiguration configuration,
    ILogger<HubNotificationJob> logger)
{
    public async Task ExecuteAsync(Guid tenantId, Guid notificationId)
    {
        var options = configuration.GetSection(HubOptions.SectionName).Get<HubOptions>() ?? new HubOptions();
        if (!options.Enabled || string.IsNullOrWhiteSpace(options.BaseUrl) || string.IsNullOrWhiteSpace(options.InstanceId))
            return;

        var notification = await dbContext.Set<Modules.Notification.Domain.Entities.Notification>()
            .IgnoreQueryFilters()
            .SingleOrDefaultAsync(item => item.TenantId == tenantId && item.Id == notificationId);
        if (notification is null)
            return;

        // Nadawcy podają raz identyfikator konta, a raz identyfikator pracownika. Sprawdzamy oba,
        // zeby powiadomienie nie przepadalo tylko dlatego, ze modul uzyl innego z nich.
        var employee = await dbContext.Set<Employee>()
            .IgnoreQueryFilters()
            .Where(item => item.TenantId == tenantId
                && (item.UserId == notification.RecipientUserId || item.Id == notification.RecipientUserId))
            .OrderByDescending(item => item.UserId == notification.RecipientUserId)
            .FirstOrDefaultAsync();
        if (employee is null || string.IsNullOrWhiteSpace(employee.Email))
        {
            logger.LogDebug(
                "Pomijam wysylke do Huba dla powiadomienia {NotificationId}: brak pracownika z adresem e-mail.",
                notificationId);
            return;
        }

        using var message = new HttpRequestMessage(
            HttpMethod.Post,
            $"{options.BaseUrl.TrimEnd('/')}/api/v1/instances/{Uri.EscapeDataString(options.InstanceId)}/notifications");
        message.Headers.Add("x-sso-client-id", options.ClientId);
        message.Headers.Add("x-sso-secret", options.ClientSecret);
        message.Content = JsonContent.Create(new
        {
            email = employee.Email,
            type = notification.Category,
            title = notification.Title,
            body = notification.Body,
            url = BuildUrl(options, notification),
            sourceRef = notification.Id.ToString(),
        });

        using var response = await httpClientFactory.CreateClient("hub-platform").SendAsync(message);
        if (response.IsSuccessStatusCode)
            return;

        // Osoba bez konta w Hubie albo bez dostepu do tej instancji to stan trwaly — ponawianie
        // nigdy sie nie powiedzie, a zaleglosci w kolejce zaslaniaja prawdziwe awarie.
        if (response.StatusCode is HttpStatusCode.NotFound or HttpStatusCode.Forbidden)
        {
            logger.LogInformation(
                "Hub odrzucil powiadomienie {NotificationId} dla {Email}: HTTP {Status}.",
                notificationId, employee.Email, (int)response.StatusCode);
            return;
        }

        var body = await response.Content.ReadAsStringAsync();
        logger.LogWarning(
            "Wysylka powiadomienia {NotificationId} do Huba nieudana: HTTP {Status} {Body}",
            notificationId, (int)response.StatusCode, body);
        response.EnsureSuccessStatusCode();
    }

    private static string? BuildUrl(HubOptions options, Modules.Notification.Domain.Entities.Notification notification)
    {
        if (string.IsNullOrWhiteSpace(options.FrontendUrl))
            return null;

        var root = options.FrontendUrl.TrimEnd('/');
        return notification.ReferenceType switch
        {
            "task" when notification.ReferenceId is Guid taskId => $"{root}/tasks/{taskId}",
            _ => root,
        };
    }
}

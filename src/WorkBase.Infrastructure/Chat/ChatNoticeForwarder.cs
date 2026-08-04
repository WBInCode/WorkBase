using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Hangfire;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using WorkBase.Contracts;
using WorkBase.Infrastructure.Persistence;
using WorkBase.Modules.Organization.Domain.Entities;

namespace WorkBase.Infrastructure.Chat;

/// <summary>
/// Konfiguracja wysyłki powiadomień do czatu WB. Cała integracja jest opcjonalna:
/// bez adresu nic się nie dzieje, więc WorkBase działa tak samo bez czatu.
/// </summary>
public sealed class ChatNoticeOptions
{
    public const string SectionName = "ChatNotices";

    public bool Enabled { get; init; }

    /// <summary>
    /// Pełny adres wraz z tokenem, wystawiony w panelu czatu
    /// (/api/v1/system-notices/&lt;token&gt;). Token jest sekretem, więc nie trafia do logów.
    /// </summary>
    public string EndpointUrl { get; init; } = "";

    /// <summary>Adres frontu WorkBase, żeby powiadomienie prowadziło do miejsca zdarzenia.</summary>
    public string FrontendUrl { get; init; } = "";
}

/// <summary>Paczka wysyłana do czatu. Nazwy pól muszą zgadzać się z walidacją po tamtej stronie.</summary>
public sealed class ChatNoticePayload
{
    [JsonPropertyName("recipients")]
    public required IReadOnlyList<string> Recipients { get; init; }

    [JsonPropertyName("title")]
    public required string Title { get; init; }

    [JsonPropertyName("body")]
    public string? Body { get; init; }

    [JsonPropertyName("url")]
    public string? Url { get; init; }
}

public sealed class ChatNoticeForwarder(
    IBackgroundJobClient jobs,
    IConfiguration configuration) : IChatNoticeForwarder
{
    public void Enqueue(Guid tenantId, Guid notificationId)
    {
        var options = configuration.GetSection(ChatNoticeOptions.SectionName).Get<ChatNoticeOptions>()
            ?? new ChatNoticeOptions();
        if (!options.Enabled || string.IsNullOrWhiteSpace(options.EndpointUrl))
            return;

        jobs.Enqueue<ChatNoticeJob>(job => job.ExecuteAsync(tenantId, notificationId));
    }
}

/// <summary>
/// Wysyła powiadomienie do czatu WB. Odbiorcę czat rozpoznaje po adresie e-mail i sam
/// sprawdza, czy należy do organizacji — adres spoza niej jest po prostu pomijany.
/// </summary>
public sealed class ChatNoticeJob(
    WorkBaseDbContext dbContext,
    IHttpClientFactory httpClientFactory,
    IConfiguration configuration,
    ILogger<ChatNoticeJob> logger)
{
    /// <summary>
    /// Czat opisuje pola opcjonalne jako „al​bo brak, albo tekst” — jawny <c>null</c> nie przechodzi
    /// walidacji i przepadłoby całe powiadomienie. Dlatego puste wartości pomijamy.
    /// </summary>
    private static readonly JsonSerializerOptions FormatWysylki = new()
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    public async Task ExecuteAsync(Guid tenantId, Guid notificationId)
    {
        var options = configuration.GetSection(ChatNoticeOptions.SectionName).Get<ChatNoticeOptions>()
            ?? new ChatNoticeOptions();
        if (!options.Enabled || string.IsNullOrWhiteSpace(options.EndpointUrl))
            return;

        var notification = await dbContext.Set<Modules.Notification.Domain.Entities.Notification>()
            .IgnoreQueryFilters()
            .SingleOrDefaultAsync(item => item.TenantId == tenantId && item.Id == notificationId);
        if (notification is null)
            return;

        // Nadawcy podają raz identyfikator konta, a raz identyfikator pracownika, więc
        // sprawdzamy oba — tak samo jak przy wysyłce do Huba.
        var employee = await dbContext.Set<Employee>()
            .IgnoreQueryFilters()
            .Where(item => item.TenantId == tenantId
                && (item.UserId == notification.RecipientUserId || item.Id == notification.RecipientUserId))
            .OrderByDescending(item => item.UserId == notification.RecipientUserId)
            .FirstOrDefaultAsync();
        if (employee is null || string.IsNullOrWhiteSpace(employee.Email))
        {
            logger.LogDebug(
                "Pomijam wysylke do czatu dla powiadomienia {NotificationId}: brak pracownika z adresem e-mail.",
                notificationId);
            return;
        }

        using var message = new HttpRequestMessage(HttpMethod.Post, options.EndpointUrl);
        message.Content = JsonContent.Create(BudujTresc(options, notification, employee.Email), options: FormatWysylki);

        using var response = await httpClientFactory.CreateClient("chat-notices").SendAsync(message);
        if (response.IsSuccessStatusCode)
            return;

        // Wyłączone lub usunięte źródło to stan trwały: ponawianie nigdy się nie powiedzie,
        // a zaległości w kolejce zasłaniają prawdziwe awarie.
        if (response.StatusCode is HttpStatusCode.NotFound or HttpStatusCode.Forbidden)
        {
            logger.LogInformation(
                "Czat odrzucil powiadomienie {NotificationId}: HTTP {Status}. Sprawdz, czy zrodlo jest wlaczone.",
                notificationId, (int)response.StatusCode);
            return;
        }

        var body = await response.Content.ReadAsStringAsync();
        logger.LogWarning(
            "Wysylka powiadomienia {NotificationId} do czatu nieudana: HTTP {Status} {Body}",
            notificationId, (int)response.StatusCode, body);
        response.EnsureSuccessStatusCode();
    }

    /// <summary>
    /// Paczka w formacie oczekiwanym przez czat. Odbiorca jest jeden, bo powiadomienie
    /// WorkBase zawsze dotyczy konkretnej osoby.
    /// </summary>
    public static ChatNoticePayload BudujTresc(
        ChatNoticeOptions options,
        Modules.Notification.Domain.Entities.Notification notification,
        string email) => new()
        {
            Recipients = [email],
            Title = notification.Title,
            Body = notification.Body,
            Url = BuildUrl(options, notification),
        };

    /// <summary>
    /// Odnośnik do miejsca zdarzenia. Czat przyjmuje wyłącznie http(s), więc inne
    /// schematy odpadłyby na walidacji i przepadłoby całe powiadomienie.
    /// </summary>
    public static string? BuildUrl(
        ChatNoticeOptions options,
        Modules.Notification.Domain.Entities.Notification notification)
    {
        if (string.IsNullOrWhiteSpace(options.FrontendUrl))
            return null;
        if (!options.FrontendUrl.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
            && !options.FrontendUrl.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            return null;

        var root = options.FrontendUrl.TrimEnd('/');
        return notification.ReferenceType switch
        {
            "task" when notification.ReferenceId is Guid taskId => $"{root}/tasks/{taskId}",
            "schedule" when notification.ReferenceId is Guid scheduleId => $"{root}/time/schedule/{scheduleId}",
            _ => root,
        };
    }
}

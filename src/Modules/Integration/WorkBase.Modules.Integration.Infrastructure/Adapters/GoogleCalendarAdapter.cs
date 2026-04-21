using System.Net.Http.Json;
using Microsoft.Extensions.Logging;
using WorkBase.Modules.Integration.Application.Adapters;

namespace WorkBase.Modules.Integration.Infrastructure.Adapters;

internal sealed class GoogleCalendarAdapter(
    IHttpClientFactory httpClientFactory,
    ILogger<GoogleCalendarAdapter> logger) : ICalendarAdapter
{
    private const string BaseUrl = "https://www.googleapis.com/calendar/v3";

    public async Task<CalendarEventResult> CreateEventAsync(string accessToken, CalendarEventRequest request, CancellationToken ct = default)
    {
        var client = CreateClient(accessToken);

        var body = new
        {
            summary = request.Title,
            description = request.Description,
            location = request.Location,
            start = new { date = request.IsAllDay ? request.StartUtc.ToString("yyyy-MM-dd") : null, dateTime = request.IsAllDay ? null : request.StartUtc.ToString("o"), timeZone = "UTC" },
            end = new { date = request.IsAllDay ? request.EndUtc.ToString("yyyy-MM-dd") : null, dateTime = request.IsAllDay ? null : request.EndUtc.ToString("o"), timeZone = "UTC" }
        };

        var response = await client.PostAsJsonAsync($"{BaseUrl}/calendars/primary/events", body, ct);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<GoogleEventResponse>(ct);
        logger.LogInformation("Created Google Calendar event {EventId}", result?.Id);

        return new CalendarEventResult(result?.Id ?? string.Empty, result?.HtmlLink);
    }

    public async Task UpdateEventAsync(string accessToken, string externalEventId, CalendarEventRequest request, CancellationToken ct = default)
    {
        var client = CreateClient(accessToken);

        var body = new
        {
            summary = request.Title,
            description = request.Description,
            location = request.Location,
            start = new { date = request.IsAllDay ? request.StartUtc.ToString("yyyy-MM-dd") : null, dateTime = request.IsAllDay ? null : request.StartUtc.ToString("o"), timeZone = "UTC" },
            end = new { date = request.IsAllDay ? request.EndUtc.ToString("yyyy-MM-dd") : null, dateTime = request.IsAllDay ? null : request.EndUtc.ToString("o"), timeZone = "UTC" }
        };

        var response = await client.PutAsJsonAsync($"{BaseUrl}/calendars/primary/events/{externalEventId}", body, ct);
        response.EnsureSuccessStatusCode();

        logger.LogInformation("Updated Google Calendar event {EventId}", externalEventId);
    }

    public async Task DeleteEventAsync(string accessToken, string externalEventId, CancellationToken ct = default)
    {
        var client = CreateClient(accessToken);

        var response = await client.DeleteAsync($"{BaseUrl}/calendars/primary/events/{externalEventId}", ct);
        response.EnsureSuccessStatusCode();

        logger.LogInformation("Deleted Google Calendar event {EventId}", externalEventId);
    }

    private HttpClient CreateClient(string accessToken)
    {
        var client = httpClientFactory.CreateClient("GoogleCalendar");
        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);
        return client;
    }

    private sealed record GoogleEventResponse(string? Id, string? HtmlLink);
}

using System.Net.Http.Json;
using Microsoft.Extensions.Logging;
using WorkBase.Modules.Integration.Application.Adapters;

namespace WorkBase.Modules.Integration.Infrastructure.Adapters;

internal sealed class Microsoft365CalendarAdapter(
    IHttpClientFactory httpClientFactory,
    ILogger<Microsoft365CalendarAdapter> logger) : ICalendarAdapter
{
    private const string BaseUrl = "https://graph.microsoft.com/v1.0";

    public async Task<CalendarEventResult> CreateEventAsync(string accessToken, CalendarEventRequest request, CancellationToken ct = default)
    {
        var client = CreateClient(accessToken);

        var body = new
        {
            subject = request.Title,
            body = new { contentType = "text", content = request.Description ?? string.Empty },
            isAllDay = request.IsAllDay,
            start = new { dateTime = request.StartUtc.ToString("o"), timeZone = "UTC" },
            end = new { dateTime = request.EndUtc.ToString("o"), timeZone = "UTC" },
            location = request.Location is not null ? new { displayName = request.Location } : null
        };

        var response = await client.PostAsJsonAsync($"{BaseUrl}/me/events", body, ct);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<GraphEventResponse>(ct);
        logger.LogInformation("Created Microsoft 365 Calendar event {EventId}", result?.Id);

        return new CalendarEventResult(result?.Id ?? string.Empty, result?.WebLink);
    }

    public async Task UpdateEventAsync(string accessToken, string externalEventId, CalendarEventRequest request, CancellationToken ct = default)
    {
        var client = CreateClient(accessToken);

        var body = new
        {
            subject = request.Title,
            body = new { contentType = "text", content = request.Description ?? string.Empty },
            isAllDay = request.IsAllDay,
            start = new { dateTime = request.StartUtc.ToString("o"), timeZone = "UTC" },
            end = new { dateTime = request.EndUtc.ToString("o"), timeZone = "UTC" }
        };

        var request2 = new HttpRequestMessage(HttpMethod.Patch, $"{BaseUrl}/me/events/{externalEventId}")
        {
            Content = JsonContent.Create(body)
        };

        var response = await client.SendAsync(request2, ct);
        response.EnsureSuccessStatusCode();

        logger.LogInformation("Updated Microsoft 365 Calendar event {EventId}", externalEventId);
    }

    public async Task DeleteEventAsync(string accessToken, string externalEventId, CancellationToken ct = default)
    {
        var client = CreateClient(accessToken);

        var response = await client.DeleteAsync($"{BaseUrl}/me/events/{externalEventId}", ct);
        response.EnsureSuccessStatusCode();

        logger.LogInformation("Deleted Microsoft 365 Calendar event {EventId}", externalEventId);
    }

    private HttpClient CreateClient(string accessToken)
    {
        var client = httpClientFactory.CreateClient("Microsoft365");
        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);
        return client;
    }

    private sealed record GraphEventResponse(string? Id, string? WebLink);
}

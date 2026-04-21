namespace WorkBase.Modules.Integration.Application.Adapters;

public sealed record CalendarEventRequest(
    string Title,
    string? Description,
    DateTime StartUtc,
    DateTime EndUtc,
    bool IsAllDay,
    string? Location);

public sealed record CalendarEventResult(
    string ExternalEventId,
    string? HtmlLink);

public interface ICalendarAdapter
{
    Task<CalendarEventResult> CreateEventAsync(string accessToken, CalendarEventRequest request, CancellationToken ct = default);
    Task UpdateEventAsync(string accessToken, string externalEventId, CalendarEventRequest request, CancellationToken ct = default);
    Task DeleteEventAsync(string accessToken, string externalEventId, CancellationToken ct = default);
}

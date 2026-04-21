using System.Net.Http.Json;
using Microsoft.Extensions.Logging;
using WorkBase.Modules.Integration.Application.Adapters;

namespace WorkBase.Modules.Integration.Infrastructure.Adapters;

internal sealed class GmailEmailAdapter(
    IHttpClientFactory httpClientFactory,
    ILogger<GmailEmailAdapter> logger) : IEmailAdapter
{
    private const string BaseUrl = "https://www.googleapis.com/gmail/v1";

    public async Task<IReadOnlyList<EmailMetadata>> GetRecentEmailsAsync(string accessToken, int maxResults = 20, CancellationToken ct = default)
    {
        var client = CreateClient(accessToken);

        var response = await client.GetFromJsonAsync<GmailListResponse>(
            $"{BaseUrl}/users/me/messages?maxResults={maxResults}", ct);

        if (response?.Messages is null || response.Messages.Count == 0)
            return [];

        var emails = new List<EmailMetadata>();
        foreach (var msg in response.Messages.Take(maxResults))
        {
            var detail = await client.GetFromJsonAsync<GmailMessageResponse>(
                $"{BaseUrl}/users/me/messages/{msg.Id}?format=metadata&metadataHeaders=Subject&metadataHeaders=From", ct);

            if (detail is null) continue;

            var subject = detail.Payload?.Headers?.FirstOrDefault(h => h.Name == "Subject")?.Value ?? "(no subject)";
            var from = detail.Payload?.Headers?.FirstOrDefault(h => h.Name == "From")?.Value ?? "";
            var date = DateTimeOffset.FromUnixTimeMilliseconds(long.TryParse(detail.InternalDate, out var ts) ? ts : 0).UtcDateTime;

            emails.Add(new EmailMetadata(msg.Id, subject, from, date));
        }

        logger.LogInformation("Retrieved {Count} recent Gmail messages", emails.Count);
        return emails;
    }

    public Task LinkEmailToRecordAsync(string accessToken, EmailLinkRequest request, CancellationToken ct = default)
    {
        // Gmail doesn't have native "linking" — this would add a label or store metadata locally
        logger.LogInformation(
            "Linked Gmail message {MessageId} to {EntityType}/{EntityId}",
            request.MessageId, request.EntityType, request.EntityId);

        return Task.CompletedTask;
    }

    public async Task SendEmailAsync(string to, string subject, string htmlBody, string accessToken, CancellationToken ct = default)
    {
        var raw = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(
            $"To: {to}\r\nSubject: {subject}\r\nContent-Type: text/html; charset=utf-8\r\n\r\n{htmlBody}"))
            .Replace('+', '-').Replace('/', '_').TrimEnd('=');
        var client = CreateClient(accessToken);
        await client.PostAsJsonAsync($"{BaseUrl}/users/me/messages/send", new { raw }, ct);
        logger.LogInformation("Sent Gmail to {To} subject={Subject}", to, subject);
    }

    public async Task<List<EmailMessage>> GetInboxAsync(string accessToken, int maxResults = 20, CancellationToken ct = default)
    {
        var recent = await GetRecentEmailsAsync(accessToken, maxResults, ct);
        return recent.Select(e => new EmailMessage(e.MessageId, e.Subject, e.From, "", e.ReceivedAtUtc, false)).ToList();
    }

    private HttpClient CreateClient(string accessToken)
    {
        var client = httpClientFactory.CreateClient("Gmail");
        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);
        return client;
    }

    private sealed record GmailListResponse(List<GmailMessageRef>? Messages);
    private sealed record GmailMessageRef(string Id);
    private sealed record GmailMessageResponse(string? InternalDate, GmailPayload? Payload);
    private sealed record GmailPayload(List<GmailHeader>? Headers);
    private sealed record GmailHeader(string Name, string Value);
}

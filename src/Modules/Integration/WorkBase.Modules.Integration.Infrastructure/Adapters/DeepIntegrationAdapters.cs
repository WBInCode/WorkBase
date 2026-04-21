using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using WorkBase.Modules.Integration.Application.Adapters;

namespace WorkBase.Modules.Integration.Infrastructure.Adapters;

public sealed class OutlookMailAdapter : IEmailAdapter
{
    private static readonly HttpClient Http = new();
    private const string GraphBase = "https://graph.microsoft.com/v1.0/me";

    public async Task SendEmailAsync(string to, string subject, string htmlBody, string accessToken, CancellationToken ct)
    {
        var body = new
        {
            message = new
            {
                subject,
                body = new { contentType = "HTML", content = htmlBody },
                toRecipients = new[] { new { emailAddress = new { address = to } } },
            },
            saveToSentItems = true,
        };
        using var req = new HttpRequestMessage(HttpMethod.Post, $"{GraphBase}/sendMail");
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        req.Content = JsonContent.Create(body);
        var resp = await Http.SendAsync(req, ct);
        resp.EnsureSuccessStatusCode();
    }

    public async Task<List<EmailMessage>> GetInboxAsync(string accessToken, int maxResults, CancellationToken ct)
    {
        using var req = new HttpRequestMessage(HttpMethod.Get, $"{GraphBase}/messages?$top={maxResults}&$select=id,subject,from,receivedDateTime,isRead,bodyPreview&$orderby=receivedDateTime desc");
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        var resp = await Http.SendAsync(req, ct);
        resp.EnsureSuccessStatusCode();
        var json = await resp.Content.ReadFromJsonAsync<JsonElement>(ct);
        return json.GetProperty("value").EnumerateArray().Select(m => new EmailMessage(
            m.GetProperty("id").GetString()!,
            m.GetProperty("subject").GetString() ?? "",
            m.TryGetProperty("from", out var f) && f.TryGetProperty("emailAddress", out var ea) ? ea.GetProperty("address").GetString() ?? "" : "",
            m.GetProperty("bodyPreview").GetString() ?? "",
            m.GetProperty("receivedDateTime").GetDateTime(),
            m.GetProperty("isRead").GetBoolean()
        )).ToList();
    }

    public async Task<IReadOnlyList<EmailMetadata>> GetRecentEmailsAsync(string accessToken, int maxResults = 20, CancellationToken ct = default)
    {
        var inbox = await GetInboxAsync(accessToken, maxResults, ct);
        return inbox.Select(e => new EmailMetadata(e.Id, e.Subject, e.From, e.ReceivedAt)).ToList();
    }

    public Task LinkEmailToRecordAsync(string accessToken, EmailLinkRequest request, CancellationToken ct = default)
    {
        // Outlook link-to-record: store association (no-op placeholder — requires custom metadata storage)
        return Task.CompletedTask;
    }
}

public sealed class SlackFullAdapter : IFullChatAdapter
{
    private static readonly HttpClient Http = new();

    public async Task SendMessageAsync(string channel, string text, string accessToken, CancellationToken ct)
    {
        using var req = new HttpRequestMessage(HttpMethod.Post, "https://slack.com/api/chat.postMessage");
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        req.Content = JsonContent.Create(new { channel, text });
        var resp = await Http.SendAsync(req, ct);
        resp.EnsureSuccessStatusCode();
    }

    public async Task<List<FullChatChannel>> GetChannelsAsync(string accessToken, CancellationToken ct)
    {
        using var req = new HttpRequestMessage(HttpMethod.Get, "https://slack.com/api/conversations.list?types=public_channel,private_channel&limit=200");
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        var resp = await Http.SendAsync(req, ct);
        resp.EnsureSuccessStatusCode();
        var json = await resp.Content.ReadFromJsonAsync<JsonElement>(ct);
        return json.GetProperty("channels").EnumerateArray().Select(c => new FullChatChannel(
            c.GetProperty("id").GetString()!,
            c.GetProperty("name").GetString()!,
            c.TryGetProperty("topic", out var t) && t.TryGetProperty("value", out var tv) ? tv.GetString() : null
        )).ToList();
    }

    public async Task<List<FullChatMessage>> GetMessagesAsync(string channel, string accessToken, int limit, CancellationToken ct)
    {
        using var req = new HttpRequestMessage(HttpMethod.Get, $"https://slack.com/api/conversations.history?channel={channel}&limit={limit}");
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        var resp = await Http.SendAsync(req, ct);
        resp.EnsureSuccessStatusCode();
        var json = await resp.Content.ReadFromJsonAsync<JsonElement>(ct);
        return json.GetProperty("messages").EnumerateArray().Select(m => new FullChatMessage(
            m.TryGetProperty("ts", out var ts) ? ts.GetString()! : "",
            m.TryGetProperty("user", out var u) ? u.GetString() ?? "" : "",
            m.GetProperty("text").GetString() ?? "",
            DateTimeOffset.FromUnixTimeSeconds((long)double.Parse(m.GetProperty("ts").GetString()!.Split('.')[0])).UtcDateTime
        )).ToList();
    }
}

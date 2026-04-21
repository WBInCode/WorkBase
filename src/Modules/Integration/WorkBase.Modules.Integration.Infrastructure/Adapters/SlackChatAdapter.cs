using System.Net.Http.Json;
using Microsoft.Extensions.Logging;
using WorkBase.Modules.Integration.Application.Adapters;

namespace WorkBase.Modules.Integration.Infrastructure.Adapters;

internal sealed class SlackChatAdapter(
    IHttpClientFactory httpClientFactory,
    ILogger<SlackChatAdapter> logger) : IChatAdapter
{
    public async Task<ChatMessageResult> SendMessageAsync(string accessTokenOrWebhookUrl, ChatMessage message, CancellationToken ct = default)
    {
        if (accessTokenOrWebhookUrl.StartsWith("https://hooks.slack.com/", StringComparison.OrdinalIgnoreCase))
        {
            return await SendViaWebhookAsync(accessTokenOrWebhookUrl, message, ct);
        }

        return await SendViaApiAsync(accessTokenOrWebhookUrl, message, ct);
    }

    private async Task<ChatMessageResult> SendViaWebhookAsync(string webhookUrl, ChatMessage message, CancellationToken ct)
    {
        var client = httpClientFactory.CreateClient("Slack");

        var body = new { text = message.Text, channel = message.Channel };
        var response = await client.PostAsJsonAsync(webhookUrl, body, ct);
        response.EnsureSuccessStatusCode();

        logger.LogInformation("Sent Slack webhook message to {Channel}", message.Channel);
        return new ChatMessageResult(Guid.NewGuid().ToString(), null);
    }

    private async Task<ChatMessageResult> SendViaApiAsync(string accessToken, ChatMessage message, CancellationToken ct)
    {
        var client = httpClientFactory.CreateClient("Slack");
        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

        var body = new
        {
            channel = message.Channel,
            text = message.Text,
            thread_ts = message.ThreadId
        };

        var response = await client.PostAsJsonAsync("https://slack.com/api/chat.postMessage", body, ct);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<SlackPostMessageResponse>(ct);
        logger.LogInformation("Sent Slack API message to {Channel}, ts={Ts}", message.Channel, result?.Ts);

        return new ChatMessageResult(result?.Ts ?? string.Empty, result?.Ts);
    }

    private sealed record SlackPostMessageResponse(bool Ok, string? Ts, string? Error);
}

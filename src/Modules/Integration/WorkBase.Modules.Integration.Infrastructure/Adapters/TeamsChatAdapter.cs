using System.Net.Http.Json;
using Microsoft.Extensions.Logging;
using WorkBase.Modules.Integration.Application.Adapters;

namespace WorkBase.Modules.Integration.Infrastructure.Adapters;

internal sealed class TeamsChatAdapter(
    IHttpClientFactory httpClientFactory,
    ILogger<TeamsChatAdapter> logger) : IChatAdapter
{
    public async Task<ChatMessageResult> SendMessageAsync(string accessTokenOrWebhookUrl, ChatMessage message, CancellationToken ct = default)
    {
        if (accessTokenOrWebhookUrl.StartsWith("https://", StringComparison.OrdinalIgnoreCase)
            && accessTokenOrWebhookUrl.Contains("webhook.office.com", StringComparison.OrdinalIgnoreCase))
        {
            return await SendViaWebhookAsync(accessTokenOrWebhookUrl, message, ct);
        }

        return await SendViaGraphApiAsync(accessTokenOrWebhookUrl, message, ct);
    }

    private async Task<ChatMessageResult> SendViaWebhookAsync(string webhookUrl, ChatMessage message, CancellationToken ct)
    {
        var client = httpClientFactory.CreateClient("Teams");

        // Adaptive Card or simple MessageCard
        var body = new
        {
            type = "message",
            attachments = new[]
            {
                new
                {
                    contentType = "application/vnd.microsoft.card.adaptive",
                    content = new
                    {
                        type = "AdaptiveCard",
                        version = "1.4",
                        body = new[]
                        {
                            new { type = "TextBlock", text = message.Text, wrap = true }
                        }
                    }
                }
            }
        };

        var response = await client.PostAsJsonAsync(webhookUrl, body, ct);
        response.EnsureSuccessStatusCode();

        logger.LogInformation("Sent Teams webhook message to channel");
        return new ChatMessageResult(Guid.NewGuid().ToString(), null);
    }

    private async Task<ChatMessageResult> SendViaGraphApiAsync(string accessToken, ChatMessage message, CancellationToken ct)
    {
        var client = httpClientFactory.CreateClient("Teams");
        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

        var body = new
        {
            body = new
            {
                content = message.Text,
                contentType = "text"
            }
        };

        // Requires channel ID — message.Channel should contain the full chat/channel ID
        var response = await client.PostAsJsonAsync(
            $"https://graph.microsoft.com/v1.0/chats/{message.Channel}/messages", body, ct);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<TeamsMessageResponse>(ct);
        logger.LogInformation("Sent Teams Graph API message, id={Id}", result?.Id);

        return new ChatMessageResult(result?.Id ?? string.Empty, null);
    }

    private sealed record TeamsMessageResponse(string? Id);
}

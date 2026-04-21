namespace WorkBase.Modules.Integration.Application.Adapters;

public sealed record ChatMessage(
    string Channel,
    string Text,
    string? ThreadId = null);

public sealed record ChatMessageResult(
    string MessageId,
    string? ThreadId);

public interface IChatAdapter
{
    Task<ChatMessageResult> SendMessageAsync(string accessTokenOrWebhookUrl, ChatMessage message, CancellationToken ct = default);
}

namespace WorkBase.Modules.Integration.Application.Adapters;

public sealed record FullChatChannel(string Id, string Name, string? Topic);
public sealed record FullChatMessage(string Id, string AuthorId, string Text, DateTime SentAt);

public interface IFullChatAdapter
{
    Task SendMessageAsync(string channel, string text, string accessToken, CancellationToken ct = default);
    Task<List<FullChatChannel>> GetChannelsAsync(string accessToken, CancellationToken ct = default);
    Task<List<FullChatMessage>> GetMessagesAsync(string channel, string accessToken, int limit, CancellationToken ct = default);
}

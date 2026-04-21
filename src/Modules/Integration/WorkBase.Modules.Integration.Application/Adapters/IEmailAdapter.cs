namespace WorkBase.Modules.Integration.Application.Adapters;

public sealed record EmailLinkRequest(
    string MessageId,
    string EntityType,
    Guid EntityId);

public sealed record EmailMetadata(
    string MessageId,
    string Subject,
    string From,
    DateTime ReceivedAtUtc);

public interface IEmailAdapter
{
    Task<IReadOnlyList<EmailMetadata>> GetRecentEmailsAsync(string accessToken, int maxResults = 20, CancellationToken ct = default);
    Task LinkEmailToRecordAsync(string accessToken, EmailLinkRequest request, CancellationToken ct = default);
    Task SendEmailAsync(string to, string subject, string htmlBody, string accessToken, CancellationToken ct = default);
    Task<List<EmailMessage>> GetInboxAsync(string accessToken, int maxResults = 20, CancellationToken ct = default);
}

public sealed record EmailMessage(string Id, string Subject, string From, string Preview, DateTime ReceivedAt, bool IsRead);

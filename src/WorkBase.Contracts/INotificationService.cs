namespace WorkBase.Contracts;

public interface INotificationService
{
    Task SendAsync(Guid tenantId, Guid recipientUserId, string title, string body, string category,
        string? referenceType = null, Guid? referenceId = null, CancellationToken ct = default);
}

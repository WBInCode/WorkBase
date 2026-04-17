namespace WorkBase.Modules.Notification.Application.Contracts;

public interface INotificationRepository
{
    Task<Domain.Entities.Notification?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<List<Domain.Entities.Notification>> GetByRecipientAsync(Guid tenantId, Guid recipientUserId, bool unreadOnly, int limit, CancellationToken ct = default);
    Task<int> GetUnreadCountAsync(Guid tenantId, Guid recipientUserId, CancellationToken ct = default);
    Task AddAsync(Domain.Entities.Notification notification, CancellationToken ct = default);
    void Update(Domain.Entities.Notification notification);
    Task MarkAllReadAsync(Guid tenantId, Guid recipientUserId, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}

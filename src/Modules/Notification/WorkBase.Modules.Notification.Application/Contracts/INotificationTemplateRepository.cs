using WorkBase.Modules.Notification.Domain.Entities;

namespace WorkBase.Modules.Notification.Application.Contracts;

public interface INotificationTemplateRepository
{
    Task<NotificationTemplate?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<List<NotificationTemplate>> GetByTenantAsync(Guid tenantId, CancellationToken ct = default);
    Task AddAsync(NotificationTemplate template, CancellationToken ct = default);
    void Remove(NotificationTemplate template);
}

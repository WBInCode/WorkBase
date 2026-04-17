using WorkBase.Modules.Notification.Domain.Entities;

namespace WorkBase.Modules.Notification.Application.Contracts;

public interface INotificationPreferenceRepository
{
    Task<List<NotificationPreference>> GetByUserAsync(Guid tenantId, Guid userId, CancellationToken ct = default);
    Task<NotificationPreference?> GetAsync(Guid tenantId, Guid userId, string category, CancellationToken ct = default);
    Task AddAsync(NotificationPreference preference, CancellationToken ct = default);
}

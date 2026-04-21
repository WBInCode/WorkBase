using WorkBase.Modules.Notification.Domain.Entities;

namespace WorkBase.Modules.Notification.Application.Contracts;

public interface IPushSubscriptionRepository
{
    Task<List<PushSubscription>> GetByUserAsync(Guid tenantId, Guid userId, CancellationToken ct = default);
    Task<PushSubscription?> GetByEndpointAsync(Guid tenantId, Guid userId, string endpoint, CancellationToken ct = default);
    Task AddAsync(PushSubscription subscription, CancellationToken ct = default);
    void Remove(PushSubscription subscription);
    Task SaveChangesAsync(CancellationToken ct = default);
}

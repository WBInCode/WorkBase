using Microsoft.EntityFrameworkCore;
using WorkBase.Infrastructure.Persistence;
using WorkBase.Modules.Notification.Application.Contracts;
using WorkBase.Modules.Notification.Domain.Entities;

namespace WorkBase.Modules.Notification.Infrastructure.Persistence;

public sealed class PushSubscriptionRepository(WorkBaseDbContext db) : IPushSubscriptionRepository
{
    public async Task<List<PushSubscription>> GetByUserAsync(Guid tenantId, Guid userId, CancellationToken ct = default)
        => await db.Set<PushSubscription>()
            .Where(s => s.TenantId == tenantId && s.UserId == userId)
            .OrderByDescending(s => s.CreatedAt)
            .ToListAsync(ct);

    public async Task<PushSubscription?> GetByEndpointAsync(Guid tenantId, Guid userId, string endpoint, CancellationToken ct = default)
        => await db.Set<PushSubscription>()
            .FirstOrDefaultAsync(s => s.TenantId == tenantId && s.UserId == userId && s.Endpoint == endpoint, ct);

    public async Task AddAsync(PushSubscription subscription, CancellationToken ct = default)
        => await db.Set<PushSubscription>().AddAsync(subscription, ct);

    public void Remove(PushSubscription subscription)
        => db.Set<PushSubscription>().Remove(subscription);

    public async Task SaveChangesAsync(CancellationToken ct = default)
        => await db.SaveChangesAsync(ct);
}

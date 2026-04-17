using Microsoft.EntityFrameworkCore;
using WorkBase.Infrastructure.Persistence;
using WorkBase.Modules.Notification.Application.Contracts;

namespace WorkBase.Modules.Notification.Infrastructure.Persistence;

public sealed class NotificationRepository(WorkBaseDbContext db) : INotificationRepository
{
    public async Task<Domain.Entities.Notification?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await db.Set<Domain.Entities.Notification>().FindAsync([id], ct);

    public async Task<List<Domain.Entities.Notification>> GetByRecipientAsync(
        Guid tenantId, Guid recipientUserId, bool unreadOnly, int limit, CancellationToken ct = default)
    {
        var query = db.Set<Domain.Entities.Notification>()
            .Where(n => n.TenantId == tenantId && n.RecipientUserId == recipientUserId);

        if (unreadOnly)
            query = query.Where(n => !n.IsRead);

        return await query
            .OrderByDescending(n => n.CreatedAt)
            .Take(limit)
            .ToListAsync(ct);
    }

    public async Task<int> GetUnreadCountAsync(Guid tenantId, Guid recipientUserId, CancellationToken ct = default)
        => await db.Set<Domain.Entities.Notification>()
            .CountAsync(n => n.TenantId == tenantId && n.RecipientUserId == recipientUserId && !n.IsRead, ct);

    public async Task AddAsync(Domain.Entities.Notification notification, CancellationToken ct = default)
        => await db.Set<Domain.Entities.Notification>().AddAsync(notification, ct);

    public void Update(Domain.Entities.Notification notification)
        => db.Set<Domain.Entities.Notification>().Update(notification);

    public async Task SaveChangesAsync(CancellationToken ct = default)
        => await db.SaveChangesAsync(ct);
}

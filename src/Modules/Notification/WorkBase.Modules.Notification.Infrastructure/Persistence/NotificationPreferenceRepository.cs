using Microsoft.EntityFrameworkCore;
using WorkBase.Infrastructure.Persistence;
using WorkBase.Modules.Notification.Application.Contracts;
using WorkBase.Modules.Notification.Domain.Entities;

namespace WorkBase.Modules.Notification.Infrastructure.Persistence;

public sealed class NotificationPreferenceRepository(WorkBaseDbContext db) : INotificationPreferenceRepository
{
    public async Task<List<NotificationPreference>> GetByUserAsync(Guid tenantId, Guid userId, CancellationToken ct = default)
        => await db.Set<NotificationPreference>()
            .Where(p => p.TenantId == tenantId && p.UserId == userId)
            .OrderBy(p => p.Category)
            .ToListAsync(ct);

    public async Task<NotificationPreference?> GetAsync(Guid tenantId, Guid userId, string category, CancellationToken ct = default)
        => await db.Set<NotificationPreference>()
            .FirstOrDefaultAsync(p => p.TenantId == tenantId && p.UserId == userId && p.Category == category, ct);

    public async Task AddAsync(NotificationPreference preference, CancellationToken ct = default)
        => await db.Set<NotificationPreference>().AddAsync(preference, ct);
}

using Microsoft.EntityFrameworkCore;
using WorkBase.Infrastructure.Persistence;
using WorkBase.Modules.Notification.Application.Contracts;
using WorkBase.Modules.Notification.Domain.Entities;

namespace WorkBase.Modules.Notification.Infrastructure.Persistence;

public sealed class NotificationTemplateRepository(WorkBaseDbContext db) : INotificationTemplateRepository
{
    public async Task<NotificationTemplate?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await db.Set<NotificationTemplate>().FirstOrDefaultAsync(t => t.Id == id, ct);

    public async Task<List<NotificationTemplate>> GetByTenantAsync(Guid tenantId, CancellationToken ct = default)
        => await db.Set<NotificationTemplate>()
            .Where(t => t.TenantId == tenantId)
            .OrderBy(t => t.Code)
            .ToListAsync(ct);

    public async Task AddAsync(NotificationTemplate template, CancellationToken ct = default)
        => await db.Set<NotificationTemplate>().AddAsync(template, ct);

    public void Remove(NotificationTemplate template)
        => db.Set<NotificationTemplate>().Remove(template);
}

using Microsoft.EntityFrameworkCore;
using WorkBase.Infrastructure.Persistence;
using WorkBase.Modules.TimeTracking.Application.Contracts;
using WorkBase.Modules.TimeTracking.Domain.Entities;

namespace WorkBase.Modules.TimeTracking.Infrastructure.Repositories;

public sealed class NfcBadgeRepository(WorkBaseDbContext db) : INfcBadgeRepository
{
    public async Task<NfcBadge?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await db.Set<NfcBadge>().FindAsync([id], ct);

    public async Task<NfcBadge?> GetByBadgeUidAsync(Guid tenantId, string badgeUid, CancellationToken ct = default)
        => await db.Set<NfcBadge>().FirstOrDefaultAsync(b => b.TenantId == tenantId && b.BadgeUid == badgeUid, ct);

    public async Task<List<NfcBadge>> GetByEmployeeAsync(Guid employeeId, CancellationToken ct = default)
        => await db.Set<NfcBadge>().Where(b => b.EmployeeId == employeeId).ToListAsync(ct);

    public async Task AddAsync(NfcBadge badge, CancellationToken ct = default)
        => await db.Set<NfcBadge>().AddAsync(badge, ct);

    public void Update(NfcBadge badge) => db.Set<NfcBadge>().Update(badge);
    public void Remove(NfcBadge badge) => db.Set<NfcBadge>().Remove(badge);
}

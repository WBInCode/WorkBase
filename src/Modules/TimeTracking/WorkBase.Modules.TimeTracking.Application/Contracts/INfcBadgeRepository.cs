using WorkBase.Modules.TimeTracking.Domain.Entities;

namespace WorkBase.Modules.TimeTracking.Application.Contracts;

public interface INfcBadgeRepository
{
    Task<NfcBadge?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<NfcBadge?> GetByBadgeUidAsync(Guid tenantId, string badgeUid, CancellationToken ct = default);
    Task<List<NfcBadge>> GetByEmployeeAsync(Guid employeeId, CancellationToken ct = default);
    Task AddAsync(NfcBadge badge, CancellationToken ct = default);
    void Update(NfcBadge badge);
    void Remove(NfcBadge badge);
}

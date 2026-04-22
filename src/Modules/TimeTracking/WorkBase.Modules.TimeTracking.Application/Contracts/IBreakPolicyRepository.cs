using WorkBase.Modules.TimeTracking.Domain.Entities;

namespace WorkBase.Modules.TimeTracking.Application.Contracts;

public interface IBreakPolicyRepository
{
    Task<List<BreakPolicy>> GetByTenantAsync(Guid tenantId, CancellationToken ct = default);
    Task<BreakPolicy?> GetByIdAsync(Guid tenantId, Guid id, CancellationToken ct = default);
    Task<BreakPolicy?> GetByTypeAsync(Guid tenantId, BreakType breakType, CancellationToken ct = default);
    Task AddAsync(BreakPolicy policy, CancellationToken ct = default);
    void Update(BreakPolicy policy);
    void Remove(BreakPolicy policy);
}

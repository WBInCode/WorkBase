using WorkBase.Modules.Leave.Domain.Entities;

namespace WorkBase.Modules.Leave.Application.Contracts;

public interface ILeavePolicyRepository
{
    Task<LeavePolicy?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<LeavePolicy?> GetActiveByTypeAsync(Guid tenantId, Guid leaveTypeId, CancellationToken cancellationToken = default);
    Task<List<LeavePolicy>> GetByTenantAsync(Guid tenantId, CancellationToken cancellationToken = default);
    Task AddAsync(LeavePolicy policy, CancellationToken cancellationToken = default);
    void Update(LeavePolicy policy);
    void Remove(LeavePolicy policy);
}

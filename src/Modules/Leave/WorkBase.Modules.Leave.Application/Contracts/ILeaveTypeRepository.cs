using WorkBase.Modules.Leave.Domain.Entities;

namespace WorkBase.Modules.Leave.Application.Contracts;

public interface ILeaveTypeRepository
{
    Task<LeaveType?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<List<LeaveType>> GetActiveByTenantAsync(Guid tenantId, CancellationToken cancellationToken = default);
    Task AddAsync(LeaveType type, CancellationToken cancellationToken = default);
    void Update(LeaveType type);
    void Remove(LeaveType type);
}

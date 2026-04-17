using WorkBase.Modules.Leave.Domain.Entities;

namespace WorkBase.Modules.Leave.Application.Contracts;

public interface ILeavePolicyRepository
{
    Task<LeavePolicy?> GetActiveByTypeAsync(Guid tenantId, Guid leaveTypeId, CancellationToken cancellationToken = default);
}

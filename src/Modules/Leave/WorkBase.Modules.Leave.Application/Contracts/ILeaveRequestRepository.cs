using WorkBase.Modules.Leave.Domain.Entities;

namespace WorkBase.Modules.Leave.Application.Contracts;

public interface ILeaveRequestRepository
{
    Task<LeaveRequest?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<List<LeaveRequest>> GetByEmployeeAsync(Guid tenantId, Guid employeeId, CancellationToken cancellationToken = default);
    Task<bool> HasOverlappingRequestAsync(Guid tenantId, Guid employeeId, DateTime startDate, DateTime endDate, Guid? excludeId = null, CancellationToken cancellationToken = default);
    Task AddAsync(LeaveRequest request, CancellationToken cancellationToken = default);
    void Update(LeaveRequest request);
}

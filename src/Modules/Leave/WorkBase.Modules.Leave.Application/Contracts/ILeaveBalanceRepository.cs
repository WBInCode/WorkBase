using WorkBase.Modules.Leave.Domain.Entities;

namespace WorkBase.Modules.Leave.Application.Contracts;

public interface ILeaveBalanceRepository
{
    Task<LeaveBalance?> GetAsync(Guid tenantId, Guid employeeId, Guid leaveTypeId, int year, CancellationToken cancellationToken = default);
    Task<List<LeaveBalance>> GetByEmployeeAsync(Guid tenantId, Guid employeeId, int year, CancellationToken cancellationToken = default);
    Task AddAsync(LeaveBalance balance, CancellationToken cancellationToken = default);
    void Update(LeaveBalance balance);
}

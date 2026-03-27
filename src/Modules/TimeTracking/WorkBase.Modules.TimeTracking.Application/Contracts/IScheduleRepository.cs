using WorkBase.Modules.TimeTracking.Domain.Entities;

namespace WorkBase.Modules.TimeTracking.Application.Contracts;

public interface IScheduleRepository
{
    Task<Schedule?> GetByIdAsync(Guid tenantId, Guid id, CancellationToken cancellationToken = default);
    Task<Schedule?> GetByDateAsync(Guid tenantId, Guid employeeId, DateOnly date, CancellationToken cancellationToken = default);
    Task<List<Schedule>> GetByDateRangeAsync(Guid tenantId, Guid employeeId, DateOnly from, DateOnly to, CancellationToken cancellationToken = default);
    Task AddAsync(Schedule schedule, CancellationToken cancellationToken = default);
    void Update(Schedule schedule);
    void Remove(Schedule schedule);
}

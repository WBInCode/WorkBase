using WorkBase.Modules.TimeTracking.Domain.Entities;

namespace WorkBase.Modules.TimeTracking.Application.Contracts;

public interface ITimeSheetRepository
{
    Task<TimeSheet?> GetByDateAsync(Guid tenantId, Guid employeeId, DateOnly date, CancellationToken cancellationToken = default);
    Task AddAsync(TimeSheet timeSheet, CancellationToken cancellationToken = default);
    void Update(TimeSheet timeSheet);
}

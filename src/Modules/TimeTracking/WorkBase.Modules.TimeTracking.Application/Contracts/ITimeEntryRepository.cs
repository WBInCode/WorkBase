using WorkBase.Modules.TimeTracking.Domain.Entities;

namespace WorkBase.Modules.TimeTracking.Application.Contracts;

public interface ITimeEntryRepository
{
    Task<TimeEntry?> GetLastEntryAsync(Guid tenantId, Guid employeeId, CancellationToken cancellationToken = default);
    Task<List<TimeEntry>> GetEntriesForDateAsync(Guid tenantId, Guid employeeId, DateOnly date, CancellationToken cancellationToken = default);
    Task AddAsync(TimeEntry entry, CancellationToken cancellationToken = default);
    Task<TimeEntry?> GetByIdAsync(Guid tenantId, Guid id, CancellationToken cancellationToken = default);
    void Delete(TimeEntry entry);
}

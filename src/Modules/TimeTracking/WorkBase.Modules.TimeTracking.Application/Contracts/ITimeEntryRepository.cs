using WorkBase.Modules.TimeTracking.Domain.Entities;

namespace WorkBase.Modules.TimeTracking.Application.Contracts;

public interface ITimeEntryRepository
{
    Task<TimeEntry?> GetLastEntryAsync(Guid tenantId, Guid employeeId, CancellationToken cancellationToken = default);
    Task<TimeEntry?> GetLastEntryTodayAsync(Guid tenantId, Guid employeeId, CancellationToken cancellationToken = default);
    Task<List<TimeEntry>> GetEntriesForDateAsync(Guid tenantId, Guid employeeId, DateOnly date, CancellationToken cancellationToken = default);

    /// <summary>Wpisy doby wraz z sąsiednimi — zmiana nocna zaczyna się dzień wcześniej.</summary>
    Task<List<TimeEntry>> GetEntriesAroundDateAsync(Guid tenantId, Guid employeeId, DateOnly date, CancellationToken cancellationToken = default);
    /// <summary>
    /// Wpisy wielu pracownikow za okres, poszerzony o dobe z kazdej strony — zmiana nocna
    /// zaczyna sie dzien wczesniej, a konczy dzien pozniej.
    /// </summary>
    Task<List<TimeEntry>> GetEntriesForEmployeesRangeAsync(
        Guid tenantId,
        IReadOnlyList<Guid> employeeIds,
        DateOnly from,
        DateOnly to,
        CancellationToken cancellationToken = default);

    Task AddAsync(TimeEntry entry, CancellationToken cancellationToken = default);
    Task<TimeEntry?> GetByIdAsync(Guid tenantId, Guid id, CancellationToken cancellationToken = default);
    void Delete(TimeEntry entry);
}

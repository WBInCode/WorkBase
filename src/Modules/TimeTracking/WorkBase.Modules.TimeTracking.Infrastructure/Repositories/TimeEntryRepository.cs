using Microsoft.EntityFrameworkCore;
using WorkBase.Infrastructure.Persistence;
using WorkBase.Modules.TimeTracking.Application.Contracts;
using WorkBase.Modules.TimeTracking.Domain.Entities;

namespace WorkBase.Modules.TimeTracking.Infrastructure.Repositories;

public sealed class TimeEntryRepository(WorkBaseDbContext dbContext) : ITimeEntryRepository
{
    public async Task<TimeEntry?> GetLastEntryAsync(
        Guid tenantId, Guid employeeId, CancellationToken cancellationToken = default)
    {
        return await dbContext.Set<TimeEntry>()
            .Where(e => e.TenantId == tenantId && e.EmployeeId == employeeId)
            .OrderByDescending(e => e.EntryTime)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<List<TimeEntry>> GetEntriesForDateAsync(
        Guid tenantId, Guid employeeId, DateOnly date, CancellationToken cancellationToken = default)
    {
        var startUtc = date.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        var endUtc = date.AddDays(1).ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);

        return await dbContext.Set<TimeEntry>()
            .Where(e => e.TenantId == tenantId
                && e.EmployeeId == employeeId
                && e.EntryTime >= startUtc
                && e.EntryTime < endUtc)
            .OrderBy(e => e.EntryTime)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(TimeEntry entry, CancellationToken cancellationToken = default)
    {
        await dbContext.Set<TimeEntry>().AddAsync(entry, cancellationToken);
    }

    public async Task<TimeEntry?> GetByIdAsync(Guid tenantId, Guid id, CancellationToken cancellationToken = default)
    {
        return await dbContext.Set<TimeEntry>()
            .FirstOrDefaultAsync(e => e.TenantId == tenantId && e.Id == id, cancellationToken);
    }

    public void Delete(TimeEntry entry)
    {
        dbContext.Set<TimeEntry>().Remove(entry);
    }
}

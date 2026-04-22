using Microsoft.EntityFrameworkCore;
using WorkBase.Infrastructure.Persistence;
using WorkBase.Modules.Leave.Application.Contracts;
using WorkBase.Modules.Leave.Domain.Entities;

namespace WorkBase.Modules.Leave.Infrastructure.Repositories;

public sealed class LeaveCalendarEntryRepository(WorkBaseDbContext dbContext) : ILeaveCalendarEntryRepository
{
    public async Task<List<LeaveCalendarEntry>> GetByTeamAsync(
        Guid tenantId, IReadOnlyList<Guid> employeeIds,
        DateTime from, DateTime to, CancellationToken cancellationToken = default)
    {
        var fromUtc = DateTime.SpecifyKind(from, DateTimeKind.Utc);
        var toUtc = DateTime.SpecifyKind(to, DateTimeKind.Utc);

        return await dbContext.Set<LeaveCalendarEntry>()
            .Where(e => e.TenantId == tenantId
                && employeeIds.Contains(e.EmployeeId)
                && e.Date >= fromUtc
                && e.Date <= toUtc)
            .OrderBy(e => e.Date)
            .ToListAsync(cancellationToken);
    }
}

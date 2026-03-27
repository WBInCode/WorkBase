using Microsoft.EntityFrameworkCore;
using WorkBase.Infrastructure.Persistence;
using WorkBase.Modules.TimeTracking.Application.Contracts;
using WorkBase.Modules.TimeTracking.Domain.Entities;

namespace WorkBase.Modules.TimeTracking.Infrastructure.Repositories;

public sealed class TimeSheetRepository(WorkBaseDbContext dbContext) : ITimeSheetRepository
{
    public async Task<TimeSheet?> GetByDateAsync(
        Guid tenantId, Guid employeeId, DateOnly date, CancellationToken cancellationToken = default)
    {
        return await dbContext.Set<TimeSheet>()
            .FirstOrDefaultAsync(ts =>
                ts.TenantId == tenantId
                && ts.EmployeeId == employeeId
                && ts.Date == date,
            cancellationToken);
    }

    public async Task<List<TimeSheet>> GetByDateRangeAsync(
        Guid tenantId, Guid employeeId, DateOnly from, DateOnly to, CancellationToken cancellationToken = default)
    {
        return await dbContext.Set<TimeSheet>()
            .Where(ts =>
                ts.TenantId == tenantId
                && ts.EmployeeId == employeeId
                && ts.Date >= from
                && ts.Date <= to)
            .OrderBy(ts => ts.Date)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(TimeSheet timeSheet, CancellationToken cancellationToken = default)
    {
        await dbContext.Set<TimeSheet>().AddAsync(timeSheet, cancellationToken);
    }

    public void Update(TimeSheet timeSheet)
    {
        dbContext.Set<TimeSheet>().Update(timeSheet);
    }
}

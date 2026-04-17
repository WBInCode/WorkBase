using Microsoft.EntityFrameworkCore;
using WorkBase.Infrastructure.Persistence;
using WorkBase.Modules.TimeTracking.Application.Contracts;
using WorkBase.Modules.TimeTracking.Domain.Entities;

namespace WorkBase.Modules.TimeTracking.Infrastructure.Repositories;

public sealed class TimeCorrectionRepository(WorkBaseDbContext dbContext) : ITimeCorrectionRepository
{
    public async Task<TimeCorrection?> GetByIdAsync(Guid tenantId, Guid id, CancellationToken cancellationToken = default)
        => await dbContext.Set<TimeCorrection>()
            .FirstOrDefaultAsync(c => c.TenantId == tenantId && c.Id == id, cancellationToken);

    public async Task<List<TimeCorrection>> GetByEmployeeAsync(Guid tenantId, Guid employeeId, DateOnly from, DateOnly to, CancellationToken cancellationToken = default)
        => await dbContext.Set<TimeCorrection>()
            .Where(c => c.TenantId == tenantId && c.EmployeeId == employeeId && c.Date >= from && c.Date <= to)
            .OrderByDescending(c => c.Date)
            .ToListAsync(cancellationToken);

    public async Task AddAsync(TimeCorrection correction, CancellationToken cancellationToken = default)
        => await dbContext.Set<TimeCorrection>().AddAsync(correction, cancellationToken);

    public void Remove(TimeCorrection correction)
        => dbContext.Set<TimeCorrection>().Remove(correction);
}

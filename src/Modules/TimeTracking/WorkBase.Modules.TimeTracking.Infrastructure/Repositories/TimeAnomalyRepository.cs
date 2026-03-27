using Microsoft.EntityFrameworkCore;
using WorkBase.Infrastructure.Persistence;
using WorkBase.Modules.TimeTracking.Application.Contracts;
using WorkBase.Modules.TimeTracking.Domain.Entities;

namespace WorkBase.Modules.TimeTracking.Infrastructure.Repositories;

public sealed class TimeAnomalyRepository(WorkBaseDbContext dbContext) : ITimeAnomalyRepository
{
    public async Task<List<TimeAnomaly>> GetByDateAsync(
        Guid tenantId, Guid employeeId, DateOnly date, CancellationToken cancellationToken = default)
    {
        return await dbContext.Set<TimeAnomaly>()
            .Where(a => a.TenantId == tenantId && a.EmployeeId == employeeId && a.Date == date)
            .OrderBy(a => a.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<TimeAnomaly>> GetByDateRangeAsync(
        Guid tenantId, DateOnly from, DateOnly to, AnomalyStatus? status = null,
        CancellationToken cancellationToken = default)
    {
        var query = dbContext.Set<TimeAnomaly>()
            .Where(a => a.TenantId == tenantId && a.Date >= from && a.Date <= to);

        if (status.HasValue)
            query = query.Where(a => a.Status == status.Value);

        return await query
            .OrderByDescending(a => a.Date)
            .ThenBy(a => a.EmployeeId)
            .ToListAsync(cancellationToken);
    }

    public async Task<TimeAnomaly?> GetByIdAsync(
        Guid tenantId, Guid id, CancellationToken cancellationToken = default)
    {
        return await dbContext.Set<TimeAnomaly>()
            .FirstOrDefaultAsync(a => a.TenantId == tenantId && a.Id == id, cancellationToken);
    }

    public async Task<bool> ExistsAsync(
        Guid tenantId, Guid employeeId, DateOnly date, AnomalyType type,
        CancellationToken cancellationToken = default)
    {
        return await dbContext.Set<TimeAnomaly>()
            .AnyAsync(a =>
                a.TenantId == tenantId
                && a.EmployeeId == employeeId
                && a.Date == date
                && a.Type == type,
            cancellationToken);
    }

    public async Task AddAsync(TimeAnomaly anomaly, CancellationToken cancellationToken = default)
    {
        await dbContext.Set<TimeAnomaly>().AddAsync(anomaly, cancellationToken);
    }

    public void Update(TimeAnomaly anomaly)
    {
        dbContext.Set<TimeAnomaly>().Update(anomaly);
    }
}

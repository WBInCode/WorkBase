using Microsoft.EntityFrameworkCore;
using WorkBase.Infrastructure.Persistence;
using WorkBase.Modules.TimeTracking.Application.Contracts;
using WorkBase.Modules.TimeTracking.Domain.Entities;

namespace WorkBase.Modules.TimeTracking.Infrastructure.Repositories;

public sealed class ScheduleRepository(WorkBaseDbContext dbContext) : IScheduleRepository
{
    public async Task<Schedule?> GetByIdAsync(
        Guid tenantId, Guid id, CancellationToken cancellationToken = default)
    {
        return await dbContext.Set<Schedule>()
            .FirstOrDefaultAsync(s => s.TenantId == tenantId && s.Id == id, cancellationToken);
    }

    public async Task<Schedule?> GetByDateAsync(
        Guid tenantId, Guid employeeId, DateOnly date, CancellationToken cancellationToken = default)
    {
        return await dbContext.Set<Schedule>()
            .FirstOrDefaultAsync(s =>
                s.TenantId == tenantId
                && s.EmployeeId == employeeId
                && s.Date == date,
            cancellationToken);
    }

    public async Task<List<Schedule>> GetByDateRangeAsync(
        Guid tenantId, Guid employeeId, DateOnly from, DateOnly to, CancellationToken cancellationToken = default)
    {
        return await dbContext.Set<Schedule>()
            .Where(s =>
                s.TenantId == tenantId
                && s.EmployeeId == employeeId
                && s.Date >= from
                && s.Date <= to)
            .OrderBy(s => s.Date)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<Schedule>> GetByEmployeesDateRangeAsync(
        Guid tenantId, IReadOnlyList<Guid> employeeIds, DateOnly from, DateOnly to, CancellationToken cancellationToken = default)
    {
        return await dbContext.Set<Schedule>()
            .Where(s =>
                s.TenantId == tenantId
                && employeeIds.Contains(s.EmployeeId)
                && s.Date >= from
                && s.Date <= to)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(Schedule schedule, CancellationToken cancellationToken = default)
    {
        await dbContext.Set<Schedule>().AddAsync(schedule, cancellationToken);
    }

    public async Task AddManyAsync(IEnumerable<Schedule> schedules, CancellationToken cancellationToken = default)
    {
        await dbContext.Set<Schedule>().AddRangeAsync(schedules, cancellationToken);
    }

    public void Update(Schedule schedule)
    {
        dbContext.Set<Schedule>().Update(schedule);
    }

    public void Remove(Schedule schedule)
    {
        dbContext.Set<Schedule>().Remove(schedule);
    }

    public void RemoveRange(IEnumerable<Schedule> schedules)
    {
        dbContext.Set<Schedule>().RemoveRange(schedules);
    }
}

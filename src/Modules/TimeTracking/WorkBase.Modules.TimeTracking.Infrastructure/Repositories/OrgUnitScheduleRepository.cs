using Microsoft.EntityFrameworkCore;
using WorkBase.Infrastructure.Persistence;
using WorkBase.Modules.TimeTracking.Application.Contracts;
using WorkBase.Modules.TimeTracking.Domain.Entities;

namespace WorkBase.Modules.TimeTracking.Infrastructure.Repositories;

public sealed class OrgUnitScheduleRepository(WorkBaseDbContext dbContext) : IOrgUnitScheduleRepository
{
    public async Task<OrgUnitSchedule?> GetByIdAsync(Guid tenantId, Guid id, CancellationToken cancellationToken = default)
        => await dbContext.Set<OrgUnitSchedule>()
            .FirstOrDefaultAsync(s => s.TenantId == tenantId && s.Id == id, cancellationToken);

    public async Task<OrgUnitSchedule?> GetByOrgUnitIdAsync(Guid tenantId, Guid orgUnitId, CancellationToken cancellationToken = default)
        => await dbContext.Set<OrgUnitSchedule>()
            .FirstOrDefaultAsync(s => s.TenantId == tenantId && s.OrgUnitId == orgUnitId && s.IsActive, cancellationToken);

    public async Task<List<OrgUnitSchedule>> GetAllAsync(Guid tenantId, CancellationToken cancellationToken = default)
        => await dbContext.Set<OrgUnitSchedule>()
            .Where(s => s.TenantId == tenantId)
            .OrderBy(s => s.Name)
            .ToListAsync(cancellationToken);

    public async Task AddAsync(OrgUnitSchedule schedule, CancellationToken cancellationToken = default)
        => await dbContext.Set<OrgUnitSchedule>().AddAsync(schedule, cancellationToken);

    public void Update(OrgUnitSchedule schedule) => dbContext.Set<OrgUnitSchedule>().Update(schedule);
    public void Remove(OrgUnitSchedule schedule) => dbContext.Set<OrgUnitSchedule>().Remove(schedule);
}

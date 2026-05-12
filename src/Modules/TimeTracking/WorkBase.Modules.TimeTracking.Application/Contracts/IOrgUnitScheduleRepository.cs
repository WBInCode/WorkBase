using WorkBase.Modules.TimeTracking.Domain.Entities;

namespace WorkBase.Modules.TimeTracking.Application.Contracts;

public interface IOrgUnitScheduleRepository
{
    Task<OrgUnitSchedule?> GetByIdAsync(Guid tenantId, Guid id, CancellationToken cancellationToken = default);
    Task<OrgUnitSchedule?> GetByOrgUnitIdAsync(Guid tenantId, Guid orgUnitId, CancellationToken cancellationToken = default);
    Task<List<OrgUnitSchedule>> GetAllAsync(Guid tenantId, CancellationToken cancellationToken = default);
    Task AddAsync(OrgUnitSchedule schedule, CancellationToken cancellationToken = default);
    void Update(OrgUnitSchedule schedule);
    void Remove(OrgUnitSchedule schedule);
}

using WorkBase.Modules.TimeTracking.Domain.Entities;

namespace WorkBase.Modules.TimeTracking.Application.Contracts;

public interface IScheduleTemplateRepository
{
    Task<ScheduleTemplate?> GetByIdAsync(Guid tenantId, Guid id, CancellationToken cancellationToken = default);
    Task<List<ScheduleTemplate>> GetAllAsync(Guid tenantId, CancellationToken cancellationToken = default);
    Task<bool> NameExistsAsync(Guid tenantId, string name, Guid? excludeId = null, CancellationToken cancellationToken = default);
    Task AddAsync(ScheduleTemplate template, CancellationToken cancellationToken = default);
    void Update(ScheduleTemplate template);
    void Remove(ScheduleTemplate template);
}

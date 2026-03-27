using WorkBase.Modules.TimeTracking.Domain.Entities;

namespace WorkBase.Modules.TimeTracking.Application.Contracts;

public interface ITimeAnomalyRepository
{
    Task<List<TimeAnomaly>> GetByDateAsync(Guid tenantId, Guid employeeId, DateOnly date, CancellationToken cancellationToken = default);
    Task<List<TimeAnomaly>> GetByDateRangeAsync(Guid tenantId, DateOnly from, DateOnly to, AnomalyStatus? status = null, CancellationToken cancellationToken = default);
    Task<TimeAnomaly?> GetByIdAsync(Guid tenantId, Guid id, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(Guid tenantId, Guid employeeId, DateOnly date, AnomalyType type, CancellationToken cancellationToken = default);
    Task AddAsync(TimeAnomaly anomaly, CancellationToken cancellationToken = default);
    void Update(TimeAnomaly anomaly);
}

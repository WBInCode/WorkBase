using WorkBase.Modules.TimeTracking.Domain.Entities;

namespace WorkBase.Modules.TimeTracking.Application.Contracts;

public interface ITimeCorrectionRepository
{
    Task<TimeCorrection?> GetByIdAsync(Guid tenantId, Guid id, CancellationToken cancellationToken = default);
    Task<List<TimeCorrection>> GetByEmployeeAsync(Guid tenantId, Guid employeeId, DateOnly from, DateOnly to, CancellationToken cancellationToken = default);
    Task AddAsync(TimeCorrection correction, CancellationToken cancellationToken = default);
}

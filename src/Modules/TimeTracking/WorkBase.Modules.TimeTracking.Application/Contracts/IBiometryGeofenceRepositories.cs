using WorkBase.Modules.TimeTracking.Domain.Entities;

namespace WorkBase.Modules.TimeTracking.Application.Contracts;

public interface IBiometricTemplateRepository
{
    Task<BiometricTemplate?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<BiometricTemplate?> GetByHashAsync(Guid tenantId, string templateHash, CancellationToken ct = default);
    Task<List<BiometricTemplate>> GetByEmployeeAsync(Guid employeeId, CancellationToken ct = default);
    Task AddAsync(BiometricTemplate template, CancellationToken ct = default);
    void Update(BiometricTemplate template);
}

public interface IGeofenceZoneRepository
{
    Task<GeofenceZone?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<List<GeofenceZone>> GetActiveAsync(Guid tenantId, CancellationToken ct = default);
    Task AddAsync(GeofenceZone zone, CancellationToken ct = default);
    void Update(GeofenceZone zone);
}

public interface IGeofenceEventRepository
{
    Task AddAsync(GeofenceEvent evt, CancellationToken ct = default);
    Task<List<GeofenceEvent>> GetByEmployeeAsync(Guid employeeId, DateTime from, DateTime to, CancellationToken ct = default);
}

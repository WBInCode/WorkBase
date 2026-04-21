using Microsoft.EntityFrameworkCore;
using WorkBase.Infrastructure.Persistence;
using WorkBase.Modules.TimeTracking.Application.Contracts;
using WorkBase.Modules.TimeTracking.Domain.Entities;

namespace WorkBase.Modules.TimeTracking.Infrastructure.Repositories;

public sealed class BiometricTemplateRepository(WorkBaseDbContext db) : IBiometricTemplateRepository
{
    public async Task<BiometricTemplate?> GetByIdAsync(Guid id, CancellationToken ct) => await db.Set<BiometricTemplate>().FindAsync([id], ct);
    public async Task<BiometricTemplate?> GetByHashAsync(Guid tenantId, string templateHash, CancellationToken ct) =>
        await db.Set<BiometricTemplate>().FirstOrDefaultAsync(t => t.TenantId == tenantId && t.TemplateHash == templateHash && t.IsActive, ct);
    public async Task<List<BiometricTemplate>> GetByEmployeeAsync(Guid employeeId, CancellationToken ct) =>
        await db.Set<BiometricTemplate>().Where(t => t.EmployeeId == employeeId).ToListAsync(ct);
    public async Task AddAsync(BiometricTemplate template, CancellationToken ct) => await db.Set<BiometricTemplate>().AddAsync(template, ct);
    public void Update(BiometricTemplate template) => db.Set<BiometricTemplate>().Update(template);
}

public sealed class GeofenceZoneRepository(WorkBaseDbContext db) : IGeofenceZoneRepository
{
    public async Task<GeofenceZone?> GetByIdAsync(Guid id, CancellationToken ct) => await db.Set<GeofenceZone>().FindAsync([id], ct);
    public async Task<List<GeofenceZone>> GetActiveAsync(Guid tenantId, CancellationToken ct) =>
        await db.Set<GeofenceZone>().Where(z => z.TenantId == tenantId && z.IsActive).ToListAsync(ct);
    public async Task AddAsync(GeofenceZone zone, CancellationToken ct) => await db.Set<GeofenceZone>().AddAsync(zone, ct);
    public void Update(GeofenceZone zone) => db.Set<GeofenceZone>().Update(zone);
}

public sealed class GeofenceEventRepository(WorkBaseDbContext db) : IGeofenceEventRepository
{
    public async Task AddAsync(GeofenceEvent evt, CancellationToken ct) => await db.Set<GeofenceEvent>().AddAsync(evt, ct);
    public async Task<List<GeofenceEvent>> GetByEmployeeAsync(Guid employeeId, DateTime from, DateTime to, CancellationToken ct) =>
        await db.Set<GeofenceEvent>().Where(e => e.EmployeeId == employeeId && e.OccurredAt >= from && e.OccurredAt <= to)
            .OrderByDescending(e => e.OccurredAt).ToListAsync(ct);
}

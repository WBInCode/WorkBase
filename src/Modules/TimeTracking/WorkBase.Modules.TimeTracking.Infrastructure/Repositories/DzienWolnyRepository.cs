using Microsoft.EntityFrameworkCore;
using WorkBase.Infrastructure.Persistence;
using WorkBase.Modules.TimeTracking.Application.Contracts;
using WorkBase.Modules.TimeTracking.Domain.Entities;

namespace WorkBase.Modules.TimeTracking.Infrastructure.Repositories;

public sealed class DzienWolnyRepository(WorkBaseDbContext dbContext) : IDzienWolnyRepository
{
    public async Task<List<DzienWolny>> PobierzZakresAsync(
        Guid tenantId, DateOnly od, DateOnly do_, CancellationToken cancellationToken = default)
    {
        return await dbContext.Set<DzienWolny>()
            .Where(d => d.TenantId == tenantId && d.Data >= od && d.Data <= do_)
            .OrderBy(d => d.Data)
            .ToListAsync(cancellationToken);
    }

    public async Task<DzienWolny?> PobierzAsync(
        Guid tenantId, Guid id, CancellationToken cancellationToken = default)
        => await dbContext.Set<DzienWolny>()
            .FirstOrDefaultAsync(d => d.TenantId == tenantId && d.Id == id, cancellationToken);

    public async Task<bool> IstniejeWDniuAsync(
        Guid tenantId, DateOnly data, CancellationToken cancellationToken = default)
        => await dbContext.Set<DzienWolny>()
            .AnyAsync(d => d.TenantId == tenantId && d.Data == data, cancellationToken);

    public async Task DodajAsync(DzienWolny dzien, CancellationToken cancellationToken = default)
        => await dbContext.Set<DzienWolny>().AddAsync(dzien, cancellationToken);

    public void Usun(DzienWolny dzien) => dbContext.Set<DzienWolny>().Remove(dzien);
}

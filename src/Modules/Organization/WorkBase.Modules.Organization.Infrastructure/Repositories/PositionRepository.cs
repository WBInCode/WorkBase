using Microsoft.EntityFrameworkCore;
using WorkBase.Infrastructure.Persistence;
using WorkBase.Modules.Organization.Application.Contracts;
using WorkBase.Modules.Organization.Domain.Entities;

namespace WorkBase.Modules.Organization.Infrastructure.Repositories;

public sealed class PositionRepository(WorkBaseDbContext dbContext) : IPositionRepository
{
    public async Task<Position?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await dbContext.Set<Position>()
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
    }

    public async Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await dbContext.Set<Position>()
            .AnyAsync(p => p.Id == id, cancellationToken);
    }

    public async Task<bool> NameExistsInTenantAsync(Guid tenantId, string name, Guid? excludeId = null, CancellationToken cancellationToken = default)
    {
        var query = dbContext.Set<Position>()
            .Where(p => p.TenantId == tenantId && p.Name == name);

        if (excludeId.HasValue)
            query = query.Where(p => p.Id != excludeId.Value);

        return await query.AnyAsync(cancellationToken);
    }

    public async Task<List<Position>> GetAllByTenantAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        return await dbContext.Set<Position>()
            .Where(p => p.TenantId == tenantId)
            .OrderBy(p => p.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(Position position, CancellationToken cancellationToken = default)
    {
        await dbContext.Set<Position>().AddAsync(position, cancellationToken);
    }

    public void Update(Position position)
    {
        dbContext.Set<Position>().Update(position);
    }

    public void Remove(Position position)
    {
        dbContext.Set<Position>().Remove(position);
    }
}

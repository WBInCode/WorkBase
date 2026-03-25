using Microsoft.EntityFrameworkCore;
using WorkBase.Infrastructure.Persistence;
using WorkBase.Modules.Organization.Application.Contracts;
using WorkBase.Modules.Organization.Domain.Entities;

namespace WorkBase.Modules.Organization.Infrastructure.Repositories;

public sealed class OrganizationUnitTypeRepository(WorkBaseDbContext dbContext) : IOrganizationUnitTypeRepository
{
    public async Task<OrganizationUnitType?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await dbContext.Set<OrganizationUnitType>()
            .FirstOrDefaultAsync(t => t.Id == id, cancellationToken);
    }

    public async Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await dbContext.Set<OrganizationUnitType>()
            .AnyAsync(t => t.Id == id, cancellationToken);
    }

    public async Task<bool> NameExistsInTenantAsync(Guid tenantId, string name, Guid? excludeId = null, CancellationToken cancellationToken = default)
    {
        var query = dbContext.Set<OrganizationUnitType>()
            .Where(t => t.TenantId == tenantId && t.Name == name);

        if (excludeId.HasValue)
            query = query.Where(t => t.Id != excludeId.Value);

        return await query.AnyAsync(cancellationToken);
    }

    public async Task<List<OrganizationUnitType>> GetAllByTenantAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        return await dbContext.Set<OrganizationUnitType>()
            .Where(t => t.TenantId == tenantId)
            .OrderBy(t => t.SortOrder)
            .ThenBy(t => t.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(OrganizationUnitType unitType, CancellationToken cancellationToken = default)
    {
        await dbContext.Set<OrganizationUnitType>().AddAsync(unitType, cancellationToken);
    }

    public void Update(OrganizationUnitType unitType)
    {
        dbContext.Set<OrganizationUnitType>().Update(unitType);
    }
}

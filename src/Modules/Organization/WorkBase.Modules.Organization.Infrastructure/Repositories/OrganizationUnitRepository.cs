using Microsoft.EntityFrameworkCore;
using WorkBase.Infrastructure.Persistence;
using WorkBase.Modules.Organization.Application.Contracts;
using WorkBase.Modules.Organization.Domain.Entities;

namespace WorkBase.Modules.Organization.Infrastructure.Repositories;

public sealed class OrganizationUnitRepository(WorkBaseDbContext dbContext) : IOrganizationUnitRepository
{
    public async Task<OrganizationUnit?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await dbContext.Set<OrganizationUnit>()
            .FirstOrDefaultAsync(u => u.Id == id, cancellationToken);
    }

    public async Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await dbContext.Set<OrganizationUnit>()
            .AnyAsync(u => u.Id == id, cancellationToken);
    }

    public async Task<bool> NameExistsInTenantAsync(Guid tenantId, string name, Guid? excludeId = null, CancellationToken cancellationToken = default)
    {
        var query = dbContext.Set<OrganizationUnit>()
            .Where(u => u.TenantId == tenantId && u.Name == name);

        if (excludeId.HasValue)
            query = query.Where(u => u.Id != excludeId.Value);

        return await query.AnyAsync(cancellationToken);
    }

    public async Task<bool> CodeExistsInTenantAsync(Guid tenantId, string code, Guid? excludeId = null, CancellationToken cancellationToken = default)
    {
        var query = dbContext.Set<OrganizationUnit>()
            .Where(u => u.TenantId == tenantId && u.Code == code);

        if (excludeId.HasValue)
            query = query.Where(u => u.Id != excludeId.Value);

        return await query.AnyAsync(cancellationToken);
    }

    public async Task AddAsync(OrganizationUnit unit, CancellationToken cancellationToken = default)
    {
        await dbContext.Set<OrganizationUnit>().AddAsync(unit, cancellationToken);
    }

    public void Update(OrganizationUnit unit)
    {
        dbContext.Set<OrganizationUnit>().Update(unit);
    }

    public void Remove(OrganizationUnit unit)
    {
        dbContext.Set<OrganizationUnit>().Remove(unit);
    }

    public async Task<List<OrganizationUnit>> GetChildrenAsync(Guid parentId, CancellationToken cancellationToken = default)
    {
        return await dbContext.Set<OrganizationUnit>()
            .Where(u => u.ParentId == parentId)
            .ToListAsync(cancellationToken);
    }

    public async Task InsertClosureForNewUnitAsync(Guid unitId, Guid? parentId, CancellationToken cancellationToken = default)
    {
        // Self-reference: every node is its own ancestor at depth 0
        var selfClosure = OrganizationUnitClosure.Create(unitId, unitId, 0);
        await dbContext.Set<OrganizationUnitClosure>().AddAsync(selfClosure, cancellationToken);

        if (parentId.HasValue)
        {
            // Copy all ancestor->parent closure rows, replacing descendant with new unit and incrementing depth
            var parentClosures = await dbContext.Set<OrganizationUnitClosure>()
                .Where(c => c.DescendantId == parentId.Value)
                .ToListAsync(cancellationToken);

            foreach (var pc in parentClosures)
            {
                var closure = OrganizationUnitClosure.Create(pc.AncestorId, unitId, pc.Depth + 1);
                await dbContext.Set<OrganizationUnitClosure>().AddAsync(closure, cancellationToken);
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task RebuildClosureAsync(Guid unitId, Guid? oldParentId, Guid? newParentId, CancellationToken cancellationToken = default)
    {
        // Get all descendants of the unit being moved (via closure table)
        var descendantIds = await dbContext.Set<OrganizationUnitClosure>()
            .Where(c => c.AncestorId == unitId)
            .Select(c => c.DescendantId)
            .ToListAsync(cancellationToken);

        // Remove all closure entries where descendant is in the subtree AND ancestor is NOT in the subtree
        // (i.e., remove connections to the old parent chain)
        var toRemove = await dbContext.Set<OrganizationUnitClosure>()
            .Where(c => descendantIds.Contains(c.DescendantId) && !descendantIds.Contains(c.AncestorId))
            .ToListAsync(cancellationToken);

        dbContext.Set<OrganizationUnitClosure>().RemoveRange(toRemove);

        // If there's a new parent, create new closure entries connecting subtree to new ancestor chain
        if (newParentId.HasValue)
        {
            var newParentClosures = await dbContext.Set<OrganizationUnitClosure>()
                .Where(c => c.DescendantId == newParentId.Value)
                .ToListAsync(cancellationToken);

            var subtreeClosures = await dbContext.Set<OrganizationUnitClosure>()
                .Where(c => c.AncestorId == unitId)
                .ToListAsync(cancellationToken);

            foreach (var pc in newParentClosures)
            {
                foreach (var sc in subtreeClosures)
                {
                    var closure = OrganizationUnitClosure.Create(
                        pc.AncestorId,
                        sc.DescendantId,
                        pc.Depth + sc.Depth + 1);
                    await dbContext.Set<OrganizationUnitClosure>().AddAsync(closure, cancellationToken);
                }
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<List<OrganizationUnit>> GetAllByTenantAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        return await dbContext.Set<OrganizationUnit>()
            .Where(u => u.TenantId == tenantId)
            .OrderBy(u => u.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<OrganizationUnitClosure>> GetClosureByTenantAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        var unitIds = dbContext.Set<OrganizationUnit>()
            .Where(u => u.TenantId == tenantId)
            .Select(u => u.Id);

        return await dbContext.Set<OrganizationUnitClosure>()
            .Where(c => unitIds.Contains(c.AncestorId))
            .ToListAsync(cancellationToken);
    }
}

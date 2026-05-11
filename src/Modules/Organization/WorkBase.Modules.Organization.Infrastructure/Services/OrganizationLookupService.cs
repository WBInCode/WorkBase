using Microsoft.EntityFrameworkCore;
using WorkBase.Contracts;
using WorkBase.Infrastructure.Persistence;
using WorkBase.Modules.Organization.Domain.Entities;

namespace WorkBase.Modules.Organization.Infrastructure.Services;

public sealed class OrganizationLookupService(WorkBaseDbContext dbContext) : IOrganizationLookupService
{
    public async Task<List<Guid>> GetEmployeeIdsByOrgUnitAsync(
        Guid tenantId, Guid orgUnitId, CancellationToken cancellationToken = default)
    {
        return await dbContext.Set<EmployeeAssignment>()
            .Where(a => a.OrganizationUnitId == orgUnitId && a.EndDate == null)
            .Select(a => a.EmployeeId)
            .Distinct()
            .ToListAsync(cancellationToken);
    }

    public async Task<List<Guid>> GetAncestorOrgUnitIdsAsync(
        Guid orgUnitId, CancellationToken cancellationToken = default)
    {
        return await dbContext.Set<OrganizationUnitClosure>()
            .Where(c => c.DescendantId == orgUnitId && c.Depth > 0)
            .OrderBy(c => c.Depth)
            .Select(c => c.AncestorId)
            .ToListAsync(cancellationToken);
    }
}

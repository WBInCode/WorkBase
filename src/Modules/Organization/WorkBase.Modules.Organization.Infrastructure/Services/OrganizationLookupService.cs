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

    public async Task<Guid?> GetUserIdByEmployeeIdAsync(
        Guid employeeId, CancellationToken cancellationToken = default)
    {
        return await dbContext.Set<Employee>()
            .Where(e => e.Id == employeeId)
            .Select(e => e.UserId)
            .FirstOrDefaultAsync(cancellationToken);
    }

    /// <remarks>
    /// <c>IgnoreQueryFilters</c> i jawny najemca, bo o adres pyta wysylka powiadomien — a ta
    /// idzie takze z zadan cyklicznych, ktore chodza poza kontekstem zadania HTTP i filtr
    /// najemcy zwrocilby im pustke dla wszystkich.
    /// </remarks>
    public async Task<string?> GetEmailByUserIdAsync(
        Guid tenantId, Guid userId, CancellationToken cancellationToken = default)
    {
        return await dbContext.Set<Employee>()
            .IgnoreQueryFilters()
            .Where(e => e.TenantId == tenantId && e.UserId == userId)
            .Select(e => e.Email)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<string?> GetEmployeeFullNameAsync(
        Guid employeeId, CancellationToken cancellationToken = default)
    {
        return await dbContext.Set<Employee>()
            .Where(e => e.Id == employeeId)
            .Select(e => e.FirstName + " " + e.LastName)
            .FirstOrDefaultAsync(cancellationToken);
    }
}

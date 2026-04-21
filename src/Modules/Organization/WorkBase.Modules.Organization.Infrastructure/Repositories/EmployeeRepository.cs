using Microsoft.EntityFrameworkCore;
using WorkBase.Infrastructure.Persistence;
using WorkBase.Modules.Organization.Application.Contracts;
using WorkBase.Modules.Organization.Domain.Entities;

namespace WorkBase.Modules.Organization.Infrastructure.Repositories;

public sealed class EmployeeRepository(WorkBaseDbContext dbContext) : IEmployeeRepository
{
    public async Task<Employee?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await dbContext.Set<Employee>()
            .FirstOrDefaultAsync(e => e.Id == id, cancellationToken);
    }

    public async Task<Employee?> GetByEmployeeNumberAsync(Guid tenantId, string employeeNumber, CancellationToken cancellationToken = default)
    {
        return await dbContext.Set<Employee>()
            .FirstOrDefaultAsync(e => e.TenantId == tenantId && e.EmployeeNumber == employeeNumber, cancellationToken);
    }

    public async Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await dbContext.Set<Employee>()
            .AnyAsync(e => e.Id == id, cancellationToken);
    }

    public async Task<bool> EmailExistsInTenantAsync(Guid tenantId, string email, Guid? excludeId = null, CancellationToken cancellationToken = default)
    {
        var query = dbContext.Set<Employee>()
            .Where(e => e.TenantId == tenantId && e.Email == email);

        if (excludeId.HasValue)
            query = query.Where(e => e.Id != excludeId.Value);

        return await query.AnyAsync(cancellationToken);
    }

    public async Task AddAsync(Employee employee, CancellationToken cancellationToken = default)
    {
        await dbContext.Set<Employee>().AddAsync(employee, cancellationToken);
    }

    public void Update(Employee employee)
    {
        dbContext.Set<Employee>().Update(employee);
    }

    public async Task<(List<Employee> Items, int TotalCount)> GetPagedAsync(
        Guid tenantId,
        string? search,
        Guid? organizationUnitId,
        EmployeeStatus? status,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query = dbContext.Set<Employee>()
            .Where(e => e.TenantId == tenantId);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var searchLower = search.ToLower();
            query = query.Where(e =>
                e.FirstName.ToLower().Contains(searchLower) ||
                e.LastName.ToLower().Contains(searchLower) ||
                e.Email.ToLower().Contains(searchLower) ||
                (e.EmployeeNumber != null && e.EmployeeNumber.ToLower().Contains(searchLower)));
        }

        if (organizationUnitId.HasValue)
        {
            var employeeIds = dbContext.Set<EmployeeAssignment>()
                .Where(a => a.OrganizationUnitId == organizationUnitId.Value && a.EndDate == null)
                .Select(a => a.EmployeeId);

            query = query.Where(e => employeeIds.Contains(e.Id));
        }

        if (status.HasValue)
            query = query.Where(e => e.Status == status.Value);

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderBy(e => e.LastName)
            .ThenBy(e => e.FirstName)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }
}

using Microsoft.EntityFrameworkCore;
using WorkBase.Contracts;
using WorkBase.Infrastructure.Persistence;
using WorkBase.Modules.Organization.Domain.Entities;

namespace WorkBase.Infrastructure.HubPlatform;

public sealed class EmployeeAccessStatusService(WorkBaseDbContext dbContext)
    : IEmployeeAccessStatusService
{
    public async Task<EmployeeAccessStatus?> GetAsync(
        Guid tenantId,
        Guid employeeId,
        CancellationToken cancellationToken = default)
    {
        var employeeExists = await dbContext.Set<Employee>()
            .IgnoreQueryFilters()
            .AnyAsync(
                employee => employee.Id == employeeId && employee.TenantId == tenantId,
                cancellationToken);
        if (!employeeExists)
            return null;

        var managedByHub = await dbContext.Set<Tenant>()
            .IgnoreQueryFilters()
            .AnyAsync(
                tenant => tenant.Id == tenantId && tenant.HubOrganizationId != null,
                cancellationToken);
        if (!managedByHub)
            return new EmployeeAccessStatus(false, null, 0, null);

        var request = await dbContext.Set<HubEmployeeAccessRequest>()
            .AsNoTracking()
            .IgnoreQueryFilters()
            .SingleOrDefaultAsync(
                item => item.TenantId == tenantId && item.EmployeeId == employeeId,
                cancellationToken);

        return request is null
            ? new EmployeeAccessStatus(true, "NotRequested", 0, null)
            : new EmployeeAccessStatus(
                true,
                request.Status.ToString(),
                request.Attempts,
                request.UpdatedAt);
    }

    public async Task<bool> RetryAsync(
        Guid tenantId,
        Guid employeeId,
        CancellationToken cancellationToken = default)
    {
        var request = await dbContext.Set<HubEmployeeAccessRequest>()
            .IgnoreQueryFilters()
            .SingleOrDefaultAsync(
                item => item.TenantId == tenantId && item.EmployeeId == employeeId,
                cancellationToken);
        if (request is null || request.Status != HubEmployeeAccessStatus.Failed)
            return false;

        request.RetryNow();
        await dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }
}
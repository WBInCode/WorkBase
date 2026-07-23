using Microsoft.EntityFrameworkCore;
using WorkBase.Contracts;
using WorkBase.Infrastructure.Persistence;
using WorkBase.Modules.Organization.Domain.Entities;

namespace WorkBase.Infrastructure.HubPlatform;

public sealed class EmployeeAccessProvisioningQueue(WorkBaseDbContext dbContext)
    : IEmployeeAccessProvisioningQueue
{
    public async Task QueueInvitationAsync(
        EmployeeAccessInvitationRequest request,
        CancellationToken cancellationToken = default)
    {
        var tenant = await dbContext.Set<Tenant>()
            .IgnoreQueryFilters()
            .Where(item => item.Id == request.TenantId)
            .Select(item => new { item.HubOrganizationId, item.HubProductInstanceId })
            .SingleOrDefaultAsync(cancellationToken);

        if (tenant?.HubOrganizationId is null || tenant.HubProductInstanceId is null)
            return;

        var alreadyQueued = await dbContext.Set<HubEmployeeAccessRequest>()
            .IgnoreQueryFilters()
            .AnyAsync(
                item => item.TenantId == request.TenantId && item.EmployeeId == request.EmployeeId,
                cancellationToken);
        if (alreadyQueued)
            return;

        dbContext.Set<HubEmployeeAccessRequest>().Add(
            HubEmployeeAccessRequest.Create(
                request.TenantId,
                request.EmployeeId,
                tenant.HubOrganizationId,
                tenant.HubProductInstanceId,
                request.Email,
                request.FirstName,
                request.LastName));
    }

    public async Task QueueRevocationAsync(
        Guid tenantId,
        Guid employeeId,
        CancellationToken cancellationToken = default)
    {
        var accessRequest = await dbContext.Set<HubEmployeeAccessRequest>()
            .IgnoreQueryFilters()
            .SingleOrDefaultAsync(
                item => item.TenantId == tenantId && item.EmployeeId == employeeId,
                cancellationToken);
        if (accessRequest is null)
        {
            var source = await (
                from employee in dbContext.Set<Employee>().IgnoreQueryFilters()
                join tenant in dbContext.Set<Tenant>().IgnoreQueryFilters()
                    on employee.TenantId equals tenant.Id
                where employee.Id == employeeId
                      && employee.TenantId == tenantId
                      && tenant.HubOrganizationId != null
                      && tenant.HubProductInstanceId != null
                select new
                {
                    Employee = employee,
                    HubOrganizationId = tenant.HubOrganizationId!,
                    HubProductInstanceId = tenant.HubProductInstanceId!,
                })
                .SingleOrDefaultAsync(cancellationToken);
            if (source is null)
                return;

            accessRequest = HubEmployeeAccessRequest.Create(
                tenantId,
                employeeId,
                source.HubOrganizationId,
                source.HubProductInstanceId,
                source.Employee.Email,
                source.Employee.FirstName,
                source.Employee.LastName);
            dbContext.Set<HubEmployeeAccessRequest>().Add(accessRequest);
        }

        accessRequest.QueueRevocation();
    }
}
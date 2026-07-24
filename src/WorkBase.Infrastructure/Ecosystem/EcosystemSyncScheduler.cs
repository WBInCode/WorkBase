using Hangfire;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using WorkBase.Contracts.Ecosystem;
using WorkBase.Infrastructure.Persistence;
using WorkBase.Modules.Organization.Domain.Entities;

namespace WorkBase.Infrastructure.Ecosystem;

public sealed class EcosystemSyncScheduler(
    IBackgroundJobClient jobs,
    WorkBaseDbContext dbContext,
    IOptions<EcosystemOptions> options) : IEcosystemSyncScheduler
{
    private readonly EcosystemOptions _options = options.Value;

    public void Enqueue(Guid tenantId, Guid employeeId)
    {
        if (!_options.Enabled || tenantId != _options.TenantId)
            return;

        jobs.Enqueue<EcosystemSnapshotJob>(job => job.ExecuteAsync(tenantId, employeeId));
    }

    public async Task EnqueueAllAsync()
    {
        if (!_options.Enabled)
            return;

        var employeeIds = await dbContext.Set<Employee>()
            .IgnoreQueryFilters()
            .Where(employee => employee.TenantId == _options.TenantId && employee.Status != EmployeeStatus.Inactive)
            .Select(employee => employee.Id)
            .ToListAsync();

        foreach (var employeeId in employeeIds)
            Enqueue(_options.TenantId, employeeId);
    }
}
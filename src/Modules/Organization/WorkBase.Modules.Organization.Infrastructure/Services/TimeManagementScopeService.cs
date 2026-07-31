using Microsoft.EntityFrameworkCore;
using WorkBase.Contracts;
using WorkBase.Infrastructure.Persistence;
using WorkBase.Modules.Organization.Domain.Entities;
using WorkBase.Shared.Auth;

namespace WorkBase.Modules.Organization.Infrastructure.Services;

public sealed class TimeManagementScopeService(
    WorkBaseDbContext dbContext,
    IPermissionService permissionService) : ITimeManagementScopeService
{
    public async Task<bool> CanManageEmployeeTimeAsync(
        Guid userId,
        Guid tenantId,
        Guid targetEmployeeId,
        CancellationToken cancellationToken = default)
    {
        var permissions = await permissionService.GetUserPermissionsAsync(userId, tenantId, cancellationToken);
        if (permissions.Contains("time.manage"))
            return true;

        if (!permissions.Contains("time.edit"))
            return false;

        var callerEmployeeId = await dbContext.Set<Employee>()
            .Where(e => e.TenantId == tenantId && e.UserId == userId)
            .Select(e => (Guid?)e.Id)
            .FirstOrDefaultAsync(cancellationToken);
        if (callerEmployeeId is null)
            return false;

        if (callerEmployeeId == targetEmployeeId)
            return true;

        var isDirectSubordinate = await dbContext.Set<SupervisorRelation>()
            .AnyAsync(
                r => r.SubordinateEmployeeId == targetEmployeeId
                    && r.SupervisorEmployeeId == callerEmployeeId
                    && r.EndDate == null,
                cancellationToken);
        if (isDirectSubordinate)
            return true;

        // Jednostki, w których kierownik zajmuje stanowisko oznaczone jako managerskie.
        var managedUnitIds = await dbContext.Set<EmployeeAssignment>()
            .Where(a => a.TenantId == tenantId && a.EmployeeId == callerEmployeeId && a.EndDate == null)
            .Join(
                dbContext.Set<Position>().Where(p => p.IsManagerial),
                a => a.PositionId,
                p => p.Id,
                (a, _) => a.OrganizationUnitId)
            .Distinct()
            .ToListAsync(cancellationToken);
        if (managedUnitIds.Count == 0)
            return false;

        return await dbContext.Set<EmployeeAssignment>()
            .AnyAsync(
                a => a.TenantId == tenantId
                    && a.EmployeeId == targetEmployeeId
                    && a.EndDate == null
                    && managedUnitIds.Contains(a.OrganizationUnitId),
                cancellationToken);
    }
}

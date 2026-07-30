using WorkBase.Modules.Organization.Application.Contracts;
using WorkBase.Modules.Organization.Domain.Entities;
using WorkBase.Shared.Auth;

namespace WorkBase.Modules.Organization.Application.Services;

/// <summary>
/// Skutki przypisania pracownika na stanowisko: rola WorkBase ze stanowiska oraz przełożeństwo
/// w jednostce. Dzięki temu „Kierownik” na liście stanowisk i „Kierownik” wśród ról przestają
/// być dwoma niezależnymi bytami, które trzeba pilnować ręcznie.
/// </summary>
public sealed class PositionAssignmentPolicy(
    IEmployeeRepository employeeRepository,
    IEmployeeAssignmentRepository assignmentRepository,
    IPositionRepository positionRepository,
    ISupervisorRelationRepository supervisorRepository,
    IRoleManagementService roleManagement)
{
    public async Task ApplyAsync(
        Employee employee,
        Guid organizationUnitId,
        Position position,
        Guid tenantId,
        CancellationToken ct = default)
    {
        if (position.DefaultRoleId is Guid roleId && employee.UserId is Guid userId)
            await roleManagement.ApplyPositionRoleAsync(userId, tenantId, roleId, ct);

        var unitMemberIds = await GetActiveUnitMemberIdsAsync(organizationUnitId, employee.Id, ct);

        if (position.IsManagerial)
        {
            foreach (var memberId in unitMemberIds)
                await SetSupervisorAsync(memberId, employee.Id, tenantId, ct);

            return;
        }

        var managerId = await FindUnitManagerAsync(unitMemberIds, tenantId, ct);
        if (managerId is Guid manager)
            await SetSupervisorAsync(employee.Id, manager, tenantId, ct);
    }

    private async Task<List<Guid>> GetActiveUnitMemberIdsAsync(Guid organizationUnitId, Guid excludedEmployeeId, CancellationToken ct)
    {
        var assignments = await assignmentRepository.GetByOrgUnitAsync(organizationUnitId, ct);
        return [.. assignments
            .Where(assignment => assignment.EndDate is null && assignment.EmployeeId != excludedEmployeeId)
            .Select(assignment => assignment.EmployeeId)
            .Distinct()];
    }

    private async Task<Guid?> FindUnitManagerAsync(IReadOnlyCollection<Guid> unitMemberIds, Guid tenantId, CancellationToken ct)
    {
        if (unitMemberIds.Count == 0) return null;

        var positions = await positionRepository.GetAllByTenantAsync(tenantId, ct);
        var managerialPositionIds = positions.Where(p => p.IsManagerial).Select(p => p.Id).ToHashSet();
        if (managerialPositionIds.Count == 0) return null;

        foreach (var memberId in unitMemberIds)
        {
            var assignment = await assignmentRepository.GetPrimaryByEmployeeAsync(memberId, ct);
            if (assignment is not null && managerialPositionIds.Contains(assignment.PositionId))
                return memberId;
        }

        return null;
    }

    private async Task SetSupervisorAsync(Guid employeeId, Guid supervisorEmployeeId, Guid tenantId, CancellationToken ct)
    {
        if (employeeId == supervisorEmployeeId) return;
        if (!await employeeRepository.ExistsAsync(supervisorEmployeeId, ct)) return;
        if (await WouldCreateCycleAsync(employeeId, supervisorEmployeeId, ct)) return;

        var current = await supervisorRepository.GetActiveBySubordinateAsync(employeeId, ct);
        if (current is not null)
        {
            if (current.SupervisorEmployeeId == supervisorEmployeeId) return;
            current.End(DateTime.UtcNow);
            supervisorRepository.Update(current);
        }

        await supervisorRepository.AddAsync(
            SupervisorRelation.Create(tenantId, supervisorEmployeeId, employeeId, DateTime.UtcNow), ct);
    }

    private async Task<bool> WouldCreateCycleAsync(Guid employeeId, Guid supervisorEmployeeId, CancellationToken ct)
    {
        var currentId = supervisorEmployeeId;
        for (var depth = 0; depth < 50; depth++)
        {
            if (currentId == employeeId) return true;

            var ancestor = await supervisorRepository.GetActiveBySubordinateAsync(currentId, ct);
            if (ancestor is null) return false;
            currentId = ancestor.SupervisorEmployeeId;
        }

        return true;
    }
}

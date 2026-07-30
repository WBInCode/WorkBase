using NSubstitute;
using WorkBase.Modules.Organization.Application.Contracts;
using WorkBase.Modules.Organization.Application.Services;
using WorkBase.Modules.Organization.Domain.Entities;
using WorkBase.Shared.Auth;
using Xunit;

namespace WorkBase.Tests.Unit.Organization;

public class PositionAssignmentPolicyTests
{
    private static readonly Guid TenantId = Guid.Parse("00000000-0000-0000-0000-000000000001");
    private static readonly Guid UnitId = Guid.Parse("00000000-0000-0000-0000-000000000020");
    private static readonly Guid RoleId = Guid.Parse("00000000-0000-0000-0000-000000000030");

    private readonly IEmployeeRepository _employeeRepo = Substitute.For<IEmployeeRepository>();
    private readonly IEmployeeAssignmentRepository _assignmentRepo = Substitute.For<IEmployeeAssignmentRepository>();
    private readonly IPositionRepository _positionRepo = Substitute.For<IPositionRepository>();
    private readonly ISupervisorRelationRepository _supervisorRepo = Substitute.For<ISupervisorRelationRepository>();
    private readonly IRoleManagementService _roleManagement = Substitute.For<IRoleManagementService>();
    private readonly PositionAssignmentPolicy _policy;

    public PositionAssignmentPolicyTests()
    {
        _employeeRepo.ExistsAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(true);
        _policy = new PositionAssignmentPolicy(
            _employeeRepo, _assignmentRepo, _positionRepo, _supervisorRepo, _roleManagement);
    }

    private static Employee CreateEmployee(Guid? userId = null) =>
        Employee.Create(TenantId, "Jan", "Kowalski", "jan@firma.pl", null, DateTime.UtcNow, userId);

    private static Position CreatePosition(Guid? defaultRoleId = null, bool isManagerial = false) =>
        Position.Create(TenantId, isManagerial ? "Kierownik" : "Pracownik", null, defaultRoleId, isManagerial);

    [Fact]
    public async Task Position_with_default_role_assigns_that_role()
    {
        var userId = Guid.NewGuid();
        var employee = CreateEmployee(userId);
        _assignmentRepo.GetByOrgUnitAsync(UnitId, Arg.Any<CancellationToken>()).Returns([]);

        await _policy.ApplyAsync(employee, UnitId, CreatePosition(RoleId), TenantId);

        await _roleManagement.Received(1).ApplyPositionRoleAsync(userId, TenantId, RoleId, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Employee_without_account_gets_no_role()
    {
        var employee = CreateEmployee();
        _assignmentRepo.GetByOrgUnitAsync(UnitId, Arg.Any<CancellationToken>()).Returns([]);

        await _policy.ApplyAsync(employee, UnitId, CreatePosition(RoleId), TenantId);

        await _roleManagement.DidNotReceive().ApplyPositionRoleAsync(
            Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Managerial_position_becomes_supervisor_of_unit_members()
    {
        var manager = CreateEmployee();
        var memberId = Guid.NewGuid();
        _assignmentRepo.GetByOrgUnitAsync(UnitId, Arg.Any<CancellationToken>()).Returns(
        [
            EmployeeAssignment.Create(TenantId, memberId, UnitId, Guid.NewGuid(), true, DateTime.UtcNow),
            EmployeeAssignment.Create(TenantId, manager.Id, UnitId, Guid.NewGuid(), true, DateTime.UtcNow),
        ]);

        await _policy.ApplyAsync(manager, UnitId, CreatePosition(isManagerial: true), TenantId);

        await _supervisorRepo.Received(1).AddAsync(
            Arg.Is<SupervisorRelation>(relation =>
                relation.SupervisorEmployeeId == manager.Id && relation.SubordinateEmployeeId == memberId),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Regular_position_inherits_supervisor_from_unit_manager()
    {
        var employee = CreateEmployee();
        var managerId = Guid.NewGuid();
        var managerialPosition = CreatePosition(isManagerial: true);
        _assignmentRepo.GetByOrgUnitAsync(UnitId, Arg.Any<CancellationToken>()).Returns(
        [
            EmployeeAssignment.Create(TenantId, managerId, UnitId, managerialPosition.Id, true, DateTime.UtcNow),
        ]);
        _positionRepo.GetAllByTenantAsync(TenantId, Arg.Any<CancellationToken>()).Returns([managerialPosition]);
        _assignmentRepo.GetPrimaryByEmployeeAsync(managerId, Arg.Any<CancellationToken>()).Returns(
            EmployeeAssignment.Create(TenantId, managerId, UnitId, managerialPosition.Id, true, DateTime.UtcNow));

        await _policy.ApplyAsync(employee, UnitId, CreatePosition(), TenantId);

        await _supervisorRepo.Received(1).AddAsync(
            Arg.Is<SupervisorRelation>(relation =>
                relation.SupervisorEmployeeId == managerId && relation.SubordinateEmployeeId == employee.Id),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Supervisor_is_not_set_when_it_would_create_a_cycle()
    {
        var manager = CreateEmployee();
        var memberId = Guid.NewGuid();
        _assignmentRepo.GetByOrgUnitAsync(UnitId, Arg.Any<CancellationToken>()).Returns(
        [
            EmployeeAssignment.Create(TenantId, memberId, UnitId, Guid.NewGuid(), true, DateTime.UtcNow),
        ]);
        // Nowy kierownik podlega osobie, którą właśnie miałby objąć — to zamknęłoby pętlę.
        _supervisorRepo.GetActiveBySubordinateAsync(manager.Id, Arg.Any<CancellationToken>()).Returns(
            SupervisorRelation.Create(TenantId, memberId, manager.Id, DateTime.UtcNow));

        await _policy.ApplyAsync(manager, UnitId, CreatePosition(isManagerial: true), TenantId);

        await _supervisorRepo.DidNotReceive().AddAsync(Arg.Any<SupervisorRelation>(), Arg.Any<CancellationToken>());
    }
}

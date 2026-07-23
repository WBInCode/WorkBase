using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using WorkBase.Infrastructure.Persistence;
using WorkBase.Modules.Organization.Domain.Entities;

namespace WorkBase.Infrastructure.Seeding;

public static class OrganizationSeeder
{
    private static readonly Guid DefaultTenantId = Guid.Parse("00000000-0000-0000-0000-000000000001");

    // Deterministic IDs
    private static readonly Guid CompanyTypeId = Guid.Parse("20000000-0000-0000-0000-000000000001");
    private static readonly Guid DepartmentTypeId = Guid.Parse("20000000-0000-0000-0000-000000000002");
    private static readonly Guid TeamTypeId = Guid.Parse("20000000-0000-0000-0000-000000000003");

    private static readonly Guid CompanyUnitId = Guid.Parse("30000000-0000-0000-0000-000000000001");
    private static readonly Guid ItDeptId = Guid.Parse("30000000-0000-0000-0000-000000000002");
    private static readonly Guid HrDeptId = Guid.Parse("30000000-0000-0000-0000-000000000003");
    private static readonly Guid DevTeamId = Guid.Parse("30000000-0000-0000-0000-000000000004");

    private static readonly Guid DevPositionId = Guid.Parse("40000000-0000-0000-0000-000000000001");
    private static readonly Guid ManagerPositionId = Guid.Parse("40000000-0000-0000-0000-000000000002");
    private static readonly Guid HrSpecialistPositionId = Guid.Parse("40000000-0000-0000-0000-000000000003");

    // Employee "Jan Nowak" already exists with this ID
    private static readonly Guid JanNowakEmployeeId = Guid.Parse("019db4ab-0cf8-7891-8026-787573dfc13c");
    private static readonly Guid JanNowakAssignmentId = Guid.Parse("50000000-0000-0000-0000-000000000001");

    /// <summary>
    /// Creates the minimal editable organization structure for a newly provisioned tenant.
    /// Existing roots are never replaced; roots created here are identified by code ROOT.
    /// </summary>
    public static async Task SeedTenantStructureAsync(
        WorkBaseDbContext dbContext,
        Guid tenantId,
        string companyName,
        ILogger logger,
        CancellationToken cancellationToken = default)
    {
        var unitTypes = await dbContext.Set<OrganizationUnitType>()
            .IgnoreQueryFilters()
            .Where(type => type.TenantId == tenantId)
            .ToListAsync(cancellationToken);

        OrganizationUnitType EnsureType(string name, string description, int sortOrder)
        {
            var existing = unitTypes.FirstOrDefault(type => type.Name == name);
            if (existing is not null) return existing;

            var created = OrganizationUnitType.Create(tenantId, name, description, sortOrder);
            unitTypes.Add(created);
            dbContext.Set<OrganizationUnitType>().Add(created);
            return created;
        }

        var companyType = EnsureType("Firma", "Główna jednostka organizacyjna", 1);
        EnsureType("Dział", "Dział organizacyjny", 2);
        EnsureType("Zespół", "Zespół roboczy", 3);
        await dbContext.SaveChangesAsync(cancellationToken);

        var root = await dbContext.Set<OrganizationUnit>()
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(
                unit => unit.TenantId == tenantId && unit.ParentId == null,
                cancellationToken);
        if (root is not null)
        {
            if (root.Code == "ROOT" && root.Name != companyName)
            {
                root.Update(companyName, root.Code, companyType.Id);
                await dbContext.SaveChangesAsync(cancellationToken);
            }
            return;
        }

        root = OrganizationUnit.Create(tenantId, companyName, "ROOT", companyType.Id, null);
        dbContext.Set<OrganizationUnit>().Add(root);
        await dbContext.SaveChangesAsync(cancellationToken);

        dbContext.Set<OrganizationUnitClosure>().Add(
            OrganizationUnitClosure.Create(root.Id, root.Id, 0));
        await dbContext.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Seeded root organization unit for tenant {TenantId}", tenantId);
    }

    public static async Task SeedAsync(WorkBaseDbContext dbContext, ILogger logger)
    {
        if (await dbContext.Set<OrganizationUnit>().AnyAsync())
        {
            logger.LogInformation("Organization data already seeded, skipping.");
            return;
        }

        logger.LogInformation("Seeding organization structure...");

        // Unit types
        var companyType = OrganizationUnitType.Create(DefaultTenantId, "Firma", "Główna jednostka organizacyjna", 1);
        var deptType = OrganizationUnitType.Create(DefaultTenantId, "Dział", "Dział organizacyjny", 2);
        var teamType = OrganizationUnitType.Create(DefaultTenantId, "Zespół", "Zespół roboczy", 3);

        SetEntityId(companyType, CompanyTypeId);
        SetEntityId(deptType, DepartmentTypeId);
        SetEntityId(teamType, TeamTypeId);

        await dbContext.Set<OrganizationUnitType>().AddRangeAsync(companyType, deptType, teamType);

        // Organization units
        var company = OrganizationUnit.Create(DefaultTenantId, "WorkBase Sp. z o.o.", "WB", CompanyTypeId, null);
        var itDept = OrganizationUnit.Create(DefaultTenantId, "Dział IT", "IT", DepartmentTypeId, CompanyUnitId);
        var hrDept = OrganizationUnit.Create(DefaultTenantId, "Dział HR", "HR", DepartmentTypeId, CompanyUnitId);
        var devTeam = OrganizationUnit.Create(DefaultTenantId, "Zespół Developerski", "DEV", TeamTypeId, ItDeptId);

        SetEntityId(company, CompanyUnitId);
        SetEntityId(itDept, ItDeptId);
        SetEntityId(hrDept, HrDeptId);
        SetEntityId(devTeam, DevTeamId);

        await dbContext.Set<OrganizationUnit>().AddRangeAsync(company, itDept, hrDept, devTeam);

        // Closure table entries for hierarchy
        var closures = new[]
        {
            OrganizationUnitClosure.Create(CompanyUnitId, CompanyUnitId, 0),
            OrganizationUnitClosure.Create(ItDeptId, ItDeptId, 0),
            OrganizationUnitClosure.Create(CompanyUnitId, ItDeptId, 1),
            OrganizationUnitClosure.Create(HrDeptId, HrDeptId, 0),
            OrganizationUnitClosure.Create(CompanyUnitId, HrDeptId, 1),
            OrganizationUnitClosure.Create(DevTeamId, DevTeamId, 0),
            OrganizationUnitClosure.Create(ItDeptId, DevTeamId, 1),
            OrganizationUnitClosure.Create(CompanyUnitId, DevTeamId, 2),
        };
        await dbContext.Set<OrganizationUnitClosure>().AddRangeAsync(closures);

        // Positions
        var devPosition = Position.Create(DefaultTenantId, "Developer", "Programista");
        var mgrPosition = Position.Create(DefaultTenantId, "Kierownik", "Kierownik działu");
        var hrPosition = Position.Create(DefaultTenantId, "Specjalista HR", "Specjalista ds. zasobów ludzkich");

        SetEntityId(devPosition, DevPositionId);
        SetEntityId(mgrPosition, ManagerPositionId);
        SetEntityId(hrPosition, HrSpecialistPositionId);

        await dbContext.Set<Position>().AddRangeAsync(devPosition, mgrPosition, hrPosition);

        // Assign Jan Nowak to Dev Team
        var janExists = await dbContext.Set<Employee>().AnyAsync(e => e.Id == JanNowakEmployeeId);
        if (janExists)
        {
            var assignment = EmployeeAssignment.Create(
                DefaultTenantId, JanNowakEmployeeId, DevTeamId,
                DevPositionId, true, DateTime.UtcNow);
            SetEntityId(assignment, JanNowakAssignmentId);
            await dbContext.Set<EmployeeAssignment>().AddAsync(assignment);
        }

        await dbContext.SaveChangesAsync();

        logger.LogInformation("Organization structure seeded: 3 unit types, 4 units, 3 positions, 1 assignment.");
    }

    private static void SetEntityId<T>(T entity, Guid id) where T : class
    {
        var prop = typeof(T).GetProperty("Id")
            ?? typeof(T).BaseType?.GetProperty("Id");
        prop?.SetValue(entity, id);
    }
}

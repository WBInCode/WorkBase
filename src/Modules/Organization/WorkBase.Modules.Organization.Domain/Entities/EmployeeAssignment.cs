using WorkBase.Shared.Domain;

namespace WorkBase.Modules.Organization.Domain.Entities;

public sealed class EmployeeAssignment : AuditableEntity<Guid>, ITenantScoped, IAuditable
{
    public Guid TenantId { get; private set; }
    public Guid EmployeeId { get; private set; }
    public Guid OrganizationUnitId { get; private set; }
    public Guid PositionId { get; private set; }
    public bool IsPrimary { get; private set; }
    public DateTime StartDate { get; private set; }
    public DateTime? EndDate { get; private set; }

    private EmployeeAssignment() { }

    public static EmployeeAssignment Create(
        Guid tenantId,
        Guid employeeId,
        Guid organizationUnitId,
        Guid positionId,
        bool isPrimary,
        DateTime startDate)
    {
        return new EmployeeAssignment
        {
            TenantId = tenantId,
            EmployeeId = employeeId,
            OrganizationUnitId = organizationUnitId,
            PositionId = positionId,
            IsPrimary = isPrimary,
            StartDate = startDate
        };
    }

    public void Update(Guid organizationUnitId, Guid positionId, bool isPrimary)
    {
        OrganizationUnitId = organizationUnitId;
        PositionId = positionId;
        IsPrimary = isPrimary;
    }

    public void End(DateTime endDate)
    {
        EndDate = endDate;
    }
}

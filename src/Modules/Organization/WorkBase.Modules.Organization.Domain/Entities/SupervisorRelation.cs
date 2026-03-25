using WorkBase.Shared.Domain;

namespace WorkBase.Modules.Organization.Domain.Entities;

public sealed class SupervisorRelation : AuditableEntity<Guid>, ITenantScoped, IAuditable
{
    public Guid TenantId { get; private set; }
    public Guid SupervisorEmployeeId { get; private set; }
    public Guid SubordinateEmployeeId { get; private set; }
    public DateTime StartDate { get; private set; }
    public DateTime? EndDate { get; private set; }

    private SupervisorRelation() { }

    public static SupervisorRelation Create(
        Guid tenantId,
        Guid supervisorEmployeeId,
        Guid subordinateEmployeeId,
        DateTime startDate)
    {
        return new SupervisorRelation
        {
            TenantId = tenantId,
            SupervisorEmployeeId = supervisorEmployeeId,
            SubordinateEmployeeId = subordinateEmployeeId,
            StartDate = startDate
        };
    }

    public void End(DateTime endDate)
    {
        EndDate = endDate;
    }
}

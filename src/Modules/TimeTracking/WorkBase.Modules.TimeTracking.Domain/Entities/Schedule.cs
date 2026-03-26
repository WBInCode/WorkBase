using WorkBase.Shared.Domain;

namespace WorkBase.Modules.TimeTracking.Domain.Entities;

public sealed class Schedule : AuditableEntity<Guid>, ITenantScoped, IAuditable
{
    public Guid TenantId { get; private set; }
    public Guid EmployeeId { get; private set; }
    public DateOnly Date { get; private set; }
    public TimeOnly PlannedStart { get; private set; }
    public TimeOnly PlannedEnd { get; private set; }
    public string? ShiftType { get; private set; }
    public Guid? TemplateId { get; private set; }

    private Schedule() { }

    public static Schedule Create(
        Guid tenantId,
        Guid employeeId,
        DateOnly date,
        TimeOnly plannedStart,
        TimeOnly plannedEnd,
        string? shiftType = null,
        Guid? templateId = null)
    {
        return new Schedule
        {
            TenantId = tenantId,
            EmployeeId = employeeId,
            Date = date,
            PlannedStart = plannedStart,
            PlannedEnd = plannedEnd,
            ShiftType = shiftType,
            TemplateId = templateId
        };
    }

    public void Update(TimeOnly plannedStart, TimeOnly plannedEnd, string? shiftType)
    {
        PlannedStart = plannedStart;
        PlannedEnd = plannedEnd;
        ShiftType = shiftType;
    }

    public TimeSpan PlannedDuration =>
        PlannedEnd > PlannedStart
            ? PlannedEnd - PlannedStart
            : TimeSpan.FromHours(24) - (PlannedStart - PlannedEnd);
}

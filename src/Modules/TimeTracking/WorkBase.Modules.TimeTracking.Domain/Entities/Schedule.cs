using WorkBase.Shared.Domain;

namespace WorkBase.Modules.TimeTracking.Domain.Entities;

public enum ScheduleSource
{
    OrgUnit = 0,
    Individual = 1,
    Unplanned = 2
}

public sealed class Schedule : AuditableEntity<Guid>, ITenantScoped, IAuditable
{
    public Guid TenantId { get; private set; }
    public Guid EmployeeId { get; private set; }
    public DateOnly Date { get; private set; }
    public TimeOnly PlannedStart { get; private set; }
    public TimeOnly PlannedEnd { get; private set; }
    public string? ShiftType { get; private set; }
    public Guid? TemplateId { get; private set; }
    public ScheduleSource Source { get; private set; } = ScheduleSource.Individual;
    public Guid? OrgUnitScheduleId { get; private set; }

    private Schedule() { }

    public static Schedule Create(
        Guid tenantId,
        Guid employeeId,
        DateOnly date,
        TimeOnly plannedStart,
        TimeOnly plannedEnd,
        string? shiftType = null,
        Guid? templateId = null,
        ScheduleSource source = ScheduleSource.Individual,
        Guid? orgUnitScheduleId = null)
    {
        return new Schedule
        {
            TenantId = tenantId,
            EmployeeId = employeeId,
            Date = date,
            PlannedStart = plannedStart,
            PlannedEnd = plannedEnd,
            ShiftType = shiftType,
            TemplateId = templateId,
            Source = source,
            OrgUnitScheduleId = orgUnitScheduleId
        };
    }

    public void Update(TimeOnly plannedStart, TimeOnly plannedEnd, string? shiftType)
    {
        PlannedStart = plannedStart;
        PlannedEnd = plannedEnd;
        ShiftType = shiftType;
        Source = ScheduleSource.Individual;
        OrgUnitScheduleId = null;
    }

    public void UpdatePlannedEnd(TimeOnly plannedEnd)
    {
        PlannedEnd = plannedEnd;
    }

    public TimeSpan PlannedDuration =>
        PlannedEnd > PlannedStart
            ? PlannedEnd - PlannedStart
            : TimeSpan.FromHours(24) - (PlannedStart - PlannedEnd);
}

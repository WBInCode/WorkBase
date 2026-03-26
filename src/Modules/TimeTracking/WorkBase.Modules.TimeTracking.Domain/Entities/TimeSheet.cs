using WorkBase.Shared.Domain;

namespace WorkBase.Modules.TimeTracking.Domain.Entities;

public sealed class TimeSheet : AuditableEntity<Guid>, ITenantScoped, IAuditable
{
    public Guid TenantId { get; private set; }
    public Guid EmployeeId { get; private set; }
    public DateOnly Date { get; private set; }
    public TimeSpan TotalWorked { get; private set; }
    public TimeSpan TotalBreaks { get; private set; }
    public TimeSpan NetWorked { get; private set; }
    public TimeSheetStatus Status { get; private set; }
    public string? Note { get; private set; }

    private TimeSheet() { }

    public static TimeSheet Create(
        Guid tenantId,
        Guid employeeId,
        DateOnly date)
    {
        return new TimeSheet
        {
            TenantId = tenantId,
            EmployeeId = employeeId,
            Date = date,
            TotalWorked = TimeSpan.Zero,
            TotalBreaks = TimeSpan.Zero,
            NetWorked = TimeSpan.Zero,
            Status = TimeSheetStatus.Incomplete
        };
    }

    public void Recalculate(TimeSpan totalWorked, TimeSpan totalBreaks)
    {
        TotalWorked = totalWorked;
        TotalBreaks = totalBreaks;
        NetWorked = totalWorked - totalBreaks;
        Status = TimeSheetStatus.Complete;
    }

    public void MarkIncomplete()
    {
        Status = TimeSheetStatus.Incomplete;
    }

    public void Approve()
    {
        Status = TimeSheetStatus.Approved;
    }

    public void UpdateNote(string? note)
    {
        Note = note;
    }
}

public enum TimeSheetStatus
{
    Incomplete,
    Complete,
    Approved
}

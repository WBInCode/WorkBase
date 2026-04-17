using WorkBase.Shared.Domain;

namespace WorkBase.Modules.Leave.Domain.Entities;

/// <summary>
/// Wpis w kalendarzu nieobecności — generowany z zatwierdzonych wniosków.
/// Umożliwia szybkie zapytania o dostępność zespołu.
/// </summary>
public sealed class LeaveCalendarEntry : Entity<Guid>, ITenantScoped
{
    public Guid TenantId { get; private set; }
    public Guid EmployeeId { get; private set; }
    public Guid LeaveRequestId { get; private set; }
    public Guid LeaveTypeId { get; private set; }
    public DateTime Date { get; private set; }
    public decimal DayFraction { get; private set; }

    private LeaveCalendarEntry() { }

    public static LeaveCalendarEntry Create(
        Guid tenantId,
        Guid employeeId,
        Guid leaveRequestId,
        Guid leaveTypeId,
        DateTime date,
        decimal dayFraction = 1.0m)
    {
        return new LeaveCalendarEntry
        {
            TenantId = tenantId,
            EmployeeId = employeeId,
            LeaveRequestId = leaveRequestId,
            LeaveTypeId = leaveTypeId,
            Date = date,
            DayFraction = dayFraction,
        };
    }
}

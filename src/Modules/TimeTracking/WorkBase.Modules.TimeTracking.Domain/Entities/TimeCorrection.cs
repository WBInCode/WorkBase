using WorkBase.Modules.TimeTracking.Domain.Events;
using WorkBase.Shared.Domain;

namespace WorkBase.Modules.TimeTracking.Domain.Entities;

public sealed class TimeCorrection : AuditableEntity<Guid>, ITenantScoped, IAuditable
{
    public Guid TenantId { get; private set; }
    public Guid EmployeeId { get; private set; }
    public Guid? TimeSheetId { get; private set; }
    public DateOnly Date { get; private set; }
    public DateTime OriginalClockIn { get; private set; }
    public DateTime OriginalClockOut { get; private set; }
    public DateTime CorrectedClockIn { get; private set; }
    public DateTime CorrectedClockOut { get; private set; }
    public string Reason { get; private set; } = null!;
    public string CorrectedBy { get; private set; } = null!;
    public CorrectionStatus Status { get; private set; }

    private TimeCorrection() { }

    public static TimeCorrection Create(
        Guid tenantId,
        Guid employeeId,
        DateOnly date,
        DateTime originalClockIn,
        DateTime originalClockOut,
        DateTime correctedClockIn,
        DateTime correctedClockOut,
        string reason,
        string correctedBy,
        Guid? timeSheetId = null)
    {
        var correction = new TimeCorrection
        {
            TenantId = tenantId,
            EmployeeId = employeeId,
            Date = date,
            OriginalClockIn = originalClockIn,
            OriginalClockOut = originalClockOut,
            CorrectedClockIn = correctedClockIn,
            CorrectedClockOut = correctedClockOut,
            Reason = reason,
            CorrectedBy = correctedBy,
            Status = CorrectionStatus.Applied,
            TimeSheetId = timeSheetId
        };

        correction.RaiseDomainEvent(new TimeCorrectedEvent(
            correction.Id, tenantId, employeeId, date, correctedBy));
        return correction;
    }
}

public enum CorrectionStatus
{
    Applied,
    Reverted
}

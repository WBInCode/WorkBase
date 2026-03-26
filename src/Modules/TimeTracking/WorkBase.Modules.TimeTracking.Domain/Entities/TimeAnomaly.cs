using WorkBase.Modules.TimeTracking.Domain.Events;
using WorkBase.Shared.Domain;

namespace WorkBase.Modules.TimeTracking.Domain.Entities;

public sealed class TimeAnomaly : AuditableEntity<Guid>, ITenantScoped, IAuditable
{
    public Guid TenantId { get; private set; }
    public Guid EmployeeId { get; private set; }
    public Guid? TimeSheetId { get; private set; }
    public DateOnly Date { get; private set; }
    public AnomalyType Type { get; private set; }
    public AnomalyStatus Status { get; private set; }
    public string? Description { get; private set; }
    public string? Details { get; private set; }
    public string? ReviewedBy { get; private set; }
    public DateTime? ReviewedAt { get; private set; }

    private TimeAnomaly() { }

    public static TimeAnomaly Create(
        Guid tenantId,
        Guid employeeId,
        DateOnly date,
        AnomalyType type,
        string? description = null,
        string? details = null,
        Guid? timeSheetId = null)
    {
        var anomaly = new TimeAnomaly
        {
            TenantId = tenantId,
            EmployeeId = employeeId,
            Date = date,
            Type = type,
            Status = AnomalyStatus.New,
            Description = description,
            Details = details,
            TimeSheetId = timeSheetId
        };

        anomaly.RaiseDomainEvent(new AnomalyDetectedEvent(anomaly.Id, tenantId, employeeId, type.ToString(), date));
        return anomaly;
    }

    public void Review(string reviewedBy)
    {
        Status = AnomalyStatus.Reviewed;
        ReviewedBy = reviewedBy;
        ReviewedAt = DateTime.UtcNow;
    }

    public void Dismiss(string reviewedBy)
    {
        Status = AnomalyStatus.Dismissed;
        ReviewedBy = reviewedBy;
        ReviewedAt = DateTime.UtcNow;
    }
}

public enum AnomalyType
{
    MissingClockOut,
    LateArrival,
    DoubleClockIn,
    ExcessiveShift,
    WorkOnDayOff,
    MissingClockIn
}

public enum AnomalyStatus
{
    New,
    Reviewed,
    Dismissed
}

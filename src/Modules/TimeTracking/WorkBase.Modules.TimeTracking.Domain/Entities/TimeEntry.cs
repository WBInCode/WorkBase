using WorkBase.Modules.TimeTracking.Domain.Events;
using WorkBase.Shared.Domain;

namespace WorkBase.Modules.TimeTracking.Domain.Entities;

public sealed class TimeEntry : AuditableEntity<Guid>, ITenantScoped, IAuditable
{
    public Guid TenantId { get; private set; }
    public Guid EmployeeId { get; private set; }
    public DateTime EntryTime { get; private set; }
    public TimeEntryType Type { get; private set; }
    public ClockMethod Method { get; private set; }
    public string? Note { get; private set; }
    public string? IpAddress { get; private set; }
    public string? Location { get; private set; }

    private TimeEntry() { }

    public static TimeEntry Create(
        Guid tenantId,
        Guid employeeId,
        DateTime entryTime,
        TimeEntryType type,
        ClockMethod method = ClockMethod.Manual,
        string? note = null,
        string? ipAddress = null,
        string? location = null)
    {
        var entry = new TimeEntry
        {
            TenantId = tenantId,
            EmployeeId = employeeId,
            EntryTime = entryTime,
            Type = type,
            Method = method,
            Note = note,
            IpAddress = ipAddress,
            Location = location
        };

        if (type == TimeEntryType.ClockIn)
            entry.RaiseDomainEvent(new ClockInRecordedEvent(entry.Id, tenantId, employeeId, entryTime));
        else if (type == TimeEntryType.ClockOut)
            entry.RaiseDomainEvent(new ClockOutRecordedEvent(entry.Id, tenantId, employeeId, entryTime));
        else if (type == TimeEntryType.BreakStart)
            entry.RaiseDomainEvent(new BreakStartedEvent(entry.Id, tenantId, employeeId, entryTime));
        else if (type == TimeEntryType.BreakEnd)
            entry.RaiseDomainEvent(new BreakEndedEvent(entry.Id, tenantId, employeeId, entryTime));

        return entry;
    }

    public void UpdateNote(string? note)
    {
        Note = note;
    }
}

public enum TimeEntryType
{
    ClockIn,
    ClockOut,
    BreakStart,
    BreakEnd
}

public enum ClockMethod
{
    Manual,
    Qr,
    Nfc,
    Kiosk,
    Geolocation
}

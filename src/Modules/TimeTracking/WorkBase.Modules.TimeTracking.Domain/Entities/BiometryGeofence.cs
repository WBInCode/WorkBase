using WorkBase.Shared.Domain;

namespace WorkBase.Modules.TimeTracking.Domain.Entities;

public sealed class BiometricTemplate : AuditableEntity<Guid>, ITenantScoped
{
    public Guid TenantId { get; private set; }
    public Guid EmployeeId { get; private set; }
    public string BiometricType { get; private set; } = default!; // "face", "fingerprint"
    public string TemplateHash { get; private set; } = default!;
    public bool IsActive { get; private set; } = true;
    public DateTime EnrolledAt { get; private set; }

    private BiometricTemplate() { }

    public static BiometricTemplate Create(Guid tenantId, Guid employeeId, string biometricType, string templateHash)
    {
        return new BiometricTemplate
        {
            Id = Guid.NewGuid(), TenantId = tenantId, EmployeeId = employeeId,
            BiometricType = biometricType, TemplateHash = templateHash,
            EnrolledAt = DateTime.UtcNow
        };
    }

    public void Deactivate() => IsActive = false;
}

public sealed class GeofenceZone : AuditableEntity<Guid>, ITenantScoped
{
    public Guid TenantId { get; private set; }
    public string Name { get; private set; } = default!;
    public double Latitude { get; private set; }
    public double Longitude { get; private set; }
    public int RadiusMeters { get; private set; }
    public bool IsActive { get; private set; } = true;
    public bool AutoClockIn { get; private set; }
    public bool AutoClockOut { get; private set; }

    private GeofenceZone() { }

    public static GeofenceZone Create(Guid tenantId, string name, double latitude, double longitude,
        int radiusMeters, bool autoClockIn = true, bool autoClockOut = true)
    {
        return new GeofenceZone
        {
            Id = Guid.NewGuid(), TenantId = tenantId, Name = name,
            Latitude = latitude, Longitude = longitude, RadiusMeters = radiusMeters,
            AutoClockIn = autoClockIn, AutoClockOut = autoClockOut
        };
    }

    public bool IsWithinZone(double lat, double lon)
    {
        const double earthRadius = 6371000; // meters
        var dLat = DegreesToRadians(lat - Latitude);
        var dLon = DegreesToRadians(lon - Longitude);
        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                Math.Cos(DegreesToRadians(Latitude)) * Math.Cos(DegreesToRadians(lat)) *
                Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        var distance = earthRadius * c;
        return distance <= RadiusMeters;
    }

    public void Update(string name, double latitude, double longitude, int radiusMeters, bool autoClockIn, bool autoClockOut)
    {
        Name = name; Latitude = latitude; Longitude = longitude;
        RadiusMeters = radiusMeters; AutoClockIn = autoClockIn; AutoClockOut = autoClockOut;
    }

    public void Deactivate() => IsActive = false;

    private static double DegreesToRadians(double degrees) => degrees * Math.PI / 180.0;
}

public sealed class GeofenceEvent : AuditableEntity<Guid>, ITenantScoped
{
    public Guid TenantId { get; private set; }
    public Guid EmployeeId { get; private set; }
    public Guid GeofenceZoneId { get; private set; }
    public string EventType { get; private set; } = default!; // "enter", "exit"
    public double Latitude { get; private set; }
    public double Longitude { get; private set; }
    public Guid? TimeEntryId { get; private set; }
    public DateTime OccurredAt { get; private set; }

    private GeofenceEvent() { }

    public static GeofenceEvent Create(Guid tenantId, Guid employeeId, Guid zoneId, string eventType,
        double latitude, double longitude, Guid? timeEntryId = null)
    {
        return new GeofenceEvent
        {
            Id = Guid.NewGuid(), TenantId = tenantId, EmployeeId = employeeId,
            GeofenceZoneId = zoneId, EventType = eventType,
            Latitude = latitude, Longitude = longitude,
            TimeEntryId = timeEntryId, OccurredAt = DateTime.UtcNow
        };
    }
}

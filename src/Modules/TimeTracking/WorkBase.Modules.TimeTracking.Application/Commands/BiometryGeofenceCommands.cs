using WorkBase.Modules.TimeTracking.Application.Contracts;
using WorkBase.Modules.TimeTracking.Domain.Entities;
using WorkBase.Shared.Cqrs;
using WorkBase.Shared.Domain;

namespace WorkBase.Modules.TimeTracking.Application.Commands;

// ─── Biometric ───

public sealed record EnrollBiometricCommand(Guid EmployeeId, string BiometricType, string TemplateHash) : ICommand<Guid>, ITenantRequest
{
    public Guid TenantId { get; set; }
}

public sealed class EnrollBiometricHandler(IBiometricTemplateRepository repo) : ICommandHandler<EnrollBiometricCommand, Guid>
{
    public async Task<Result<Guid>> Handle(EnrollBiometricCommand cmd, CancellationToken ct)
    {
        var template = BiometricTemplate.Create(cmd.TenantId, cmd.EmployeeId, cmd.BiometricType, cmd.TemplateHash);
        await repo.AddAsync(template, ct);
        return template.Id;
    }
}

public sealed record BiometricClockInCommand(string TemplateHash, double? Latitude, double? Longitude) : ICommand<Guid>, ITenantRequest
{
    public Guid TenantId { get; set; }
}

public sealed class BiometricClockInHandler(
    IBiometricTemplateRepository bioRepo,
    ITimeEntryRepository timeRepo) : ICommandHandler<BiometricClockInCommand, Guid>
{
    public async Task<Result<Guid>> Handle(BiometricClockInCommand cmd, CancellationToken ct)
    {
        var template = await bioRepo.GetByHashAsync(cmd.TenantId, cmd.TemplateHash, ct);
        if (template is null || !template.IsActive)
            return Result.Failure<Guid>(Error.NotFound("Bio.NotFound", "Szablon biometryczny nie został znaleziony lub jest nieaktywny."));

        var last = await timeRepo.GetLastEntryAsync(cmd.TenantId, template.EmployeeId, ct);
        var entryType = last is null || last.Type == TimeEntryType.ClockOut ? TimeEntryType.ClockIn : TimeEntryType.ClockOut;

        var location = cmd.Latitude.HasValue && cmd.Longitude.HasValue ? $"{cmd.Latitude},{cmd.Longitude}" : null;
        var entry = TimeEntry.Create(cmd.TenantId, template.EmployeeId, DateTime.UtcNow, entryType, ClockMethod.Biometric, location: location);
        await timeRepo.AddAsync(entry, ct);
        return entry.Id;
    }
}

// ─── Geofence ───

public sealed record CreateGeofenceZoneCommand(
    string Name, double Latitude, double Longitude, int RadiusMeters,
    bool AutoClockIn, bool AutoClockOut) : ICommand<Guid>, ITenantRequest
{
    public Guid TenantId { get; set; }
}

public sealed class CreateGeofenceZoneHandler(IGeofenceZoneRepository repo) : ICommandHandler<CreateGeofenceZoneCommand, Guid>
{
    public async Task<Result<Guid>> Handle(CreateGeofenceZoneCommand cmd, CancellationToken ct)
    {
        var zone = GeofenceZone.Create(cmd.TenantId, cmd.Name, cmd.Latitude, cmd.Longitude,
            cmd.RadiusMeters, cmd.AutoClockIn, cmd.AutoClockOut);
        await repo.AddAsync(zone, ct);
        return zone.Id;
    }
}

public sealed record GeofenceCheckInCommand(Guid EmployeeId, double Latitude, double Longitude) : ICommand<GeofenceCheckResult>, ITenantRequest
{
    public Guid TenantId { get; set; }
}

public sealed record GeofenceCheckResult(bool InZone, string? ZoneName, Guid? TimeEntryId, string? Action);

public sealed class GeofenceCheckInHandler(
    IGeofenceZoneRepository zoneRepo,
    IGeofenceEventRepository eventRepo,
    ITimeEntryRepository timeRepo) : ICommandHandler<GeofenceCheckInCommand, GeofenceCheckResult>
{
    public async Task<Result<GeofenceCheckResult>> Handle(GeofenceCheckInCommand cmd, CancellationToken ct)
    {
        var zones = await zoneRepo.GetActiveAsync(cmd.TenantId, ct);
        var matchedZone = zones.FirstOrDefault(z => z.IsWithinZone(cmd.Latitude, cmd.Longitude));

        if (matchedZone is null)
            return new GeofenceCheckResult(false, null, null, null);

        var last = await timeRepo.GetLastEntryAsync(cmd.TenantId, cmd.EmployeeId, ct);
        var isClockIn = last is null || last.Type == TimeEntryType.ClockOut;

        string? action = null;
        Guid? timeEntryId = null;

        var location = $"{cmd.Latitude},{cmd.Longitude}";

        if (isClockIn && matchedZone.AutoClockIn)
        {
            var entry = TimeEntry.Create(cmd.TenantId, cmd.EmployeeId, DateTime.UtcNow, TimeEntryType.ClockIn, ClockMethod.Geolocation, location: location);
            await timeRepo.AddAsync(entry, ct);
            timeEntryId = entry.Id;
            action = "clock_in";
        }
        else if (!isClockIn && matchedZone.AutoClockOut)
        {
            var entry = TimeEntry.Create(cmd.TenantId, cmd.EmployeeId, DateTime.UtcNow, TimeEntryType.ClockOut, ClockMethod.Geolocation, location: location);
            await timeRepo.AddAsync(entry, ct);
            timeEntryId = entry.Id;
            action = "clock_out";
        }

        var evt = GeofenceEvent.Create(cmd.TenantId, cmd.EmployeeId, matchedZone.Id,
            isClockIn ? "enter" : "exit", cmd.Latitude, cmd.Longitude, timeEntryId);
        await eventRepo.AddAsync(evt, ct);

        return new GeofenceCheckResult(true, matchedZone.Name, timeEntryId, action);
    }
}

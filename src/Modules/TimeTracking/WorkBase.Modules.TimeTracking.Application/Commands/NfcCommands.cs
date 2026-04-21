using WorkBase.Modules.TimeTracking.Application.Contracts;
using WorkBase.Modules.TimeTracking.Domain.Entities;
using WorkBase.Shared.Cqrs;
using WorkBase.Shared.Domain;

namespace WorkBase.Modules.TimeTracking.Application.Commands;

// --- Register NFC Badge ---
public sealed record RegisterNfcBadgeCommand(
    Guid EmployeeId, string BadgeUid, string? Label) : ICommand<Guid>, ITenantRequest
{
    public Guid TenantId { get; set; }
}

public sealed class RegisterNfcBadgeHandler(
    INfcBadgeRepository badgeRepo) : ICommandHandler<RegisterNfcBadgeCommand, Guid>
{
    public async Task<Result<Guid>> Handle(RegisterNfcBadgeCommand cmd, CancellationToken ct)
    {
        // Check if badge UID already registered
        var existing = await badgeRepo.GetByBadgeUidAsync(cmd.TenantId, cmd.BadgeUid, ct);
        if (existing is not null)
            return Result.Failure<Guid>(new Error("Nfc.AlreadyRegistered", "Ten identyfikator NFC jest już zarejestrowany."));

        var badge = NfcBadge.Create(cmd.TenantId, cmd.EmployeeId, cmd.BadgeUid, cmd.Label);
        await badgeRepo.AddAsync(badge, ct);
        return badge.Id;
    }
}

// --- NFC Clock-In ---
public sealed record NfcClockInCommand(string BadgeUid) : ICommand<Guid>, ITenantRequest
{
    public Guid TenantId { get; set; }
}

public sealed class NfcClockInHandler(
    INfcBadgeRepository badgeRepo,
    ITimeEntryRepository timeEntryRepo) : ICommandHandler<NfcClockInCommand, Guid>
{
    public async Task<Result<Guid>> Handle(NfcClockInCommand cmd, CancellationToken ct)
    {
        var badge = await badgeRepo.GetByBadgeUidAsync(cmd.TenantId, cmd.BadgeUid, ct);
        if (badge is null || !badge.IsActive)
            return Result.Failure<Guid>(new Error("Nfc.BadgeNotFound", "Nieznany lub nieaktywny identyfikator NFC."));

        badge.RecordUsage();
        badgeRepo.Update(badge);

        // Determine if this is clock-in or clock-out based on last entry
        var lastEntry = await timeEntryRepo.GetLastEntryAsync(cmd.TenantId, badge.EmployeeId, ct);
        var type = lastEntry?.Type == TimeEntryType.ClockIn ? TimeEntryType.ClockOut : TimeEntryType.ClockIn;

        var entry = TimeEntry.Create(cmd.TenantId, badge.EmployeeId, DateTime.UtcNow, type, ClockMethod.Nfc);
        await timeEntryRepo.AddAsync(entry, ct);
        return entry.Id;
    }
}

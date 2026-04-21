using WorkBase.Shared.Domain;

namespace WorkBase.Modules.TimeTracking.Domain.Entities;

public sealed class NfcBadge : Entity<Guid>, ITenantScoped
{
    public Guid TenantId { get; private set; }
    public Guid EmployeeId { get; private set; }
    public string BadgeUid { get; private set; } = null!; // NFC tag UID
    public string? Label { get; private set; }
    public bool IsActive { get; private set; }
    public DateTime RegisteredAt { get; private set; }
    public DateTime? LastUsedAt { get; private set; }

    private NfcBadge() { }

    public static NfcBadge Create(Guid tenantId, Guid employeeId, string badgeUid, string? label = null)
    {
        return new NfcBadge
        {
            TenantId = tenantId,
            EmployeeId = employeeId,
            BadgeUid = badgeUid,
            Label = label,
            IsActive = true,
            RegisteredAt = DateTime.UtcNow,
        };
    }

    public void RecordUsage() => LastUsedAt = DateTime.UtcNow;
    public void Deactivate() => IsActive = false;
    public void Activate() => IsActive = true;
}

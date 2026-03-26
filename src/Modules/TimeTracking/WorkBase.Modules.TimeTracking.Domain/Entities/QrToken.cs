using WorkBase.Shared.Domain;

namespace WorkBase.Modules.TimeTracking.Domain.Entities;

public sealed class QrToken : Entity<Guid>, ITenantScoped
{
    public Guid TenantId { get; private set; }
    public string Token { get; private set; } = null!;
    public DateTime ExpiresAt { get; private set; }
    public string? LocationId { get; private set; }
    public bool IsUsed { get; private set; }
    public DateTime? UsedAt { get; private set; }
    public Guid? UsedByEmployeeId { get; private set; }
    public DateTime CreatedAt { get; private set; }

    private QrToken() { }

    public static QrToken Create(
        Guid tenantId,
        string token,
        TimeSpan ttl,
        string? locationId = null)
    {
        return new QrToken
        {
            TenantId = tenantId,
            Token = token,
            ExpiresAt = DateTime.UtcNow.Add(ttl),
            LocationId = locationId,
            IsUsed = false,
            CreatedAt = DateTime.UtcNow
        };
    }

    public bool IsExpired => DateTime.UtcNow > ExpiresAt;

    public bool CanBeUsed => !IsUsed && !IsExpired;

    public void MarkUsed(Guid employeeId)
    {
        IsUsed = true;
        UsedAt = DateTime.UtcNow;
        UsedByEmployeeId = employeeId;
    }
}

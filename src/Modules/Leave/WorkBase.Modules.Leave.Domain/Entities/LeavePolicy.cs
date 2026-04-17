using WorkBase.Shared.Domain;

namespace WorkBase.Modules.Leave.Domain.Entities;

/// <summary>
/// Polityka urlopowa — definiuje reguły naliczania limitu dla danego typu.
/// Per tenant, powiązana z LeaveType.
/// </summary>
public sealed class LeavePolicy : AuditableEntity<Guid>, ITenantScoped, IAuditable
{
    public Guid TenantId { get; private set; }
    public Guid LeaveTypeId { get; private set; }
    public string Name { get; private set; } = null!;
    public int DaysPerYear { get; private set; }
    public bool AllowCarryOver { get; private set; }
    public int MaxCarryOverDays { get; private set; }
    public int? MaxConsecutiveDays { get; private set; }
    public int? MinNoticeDays { get; private set; }
    public bool IsActive { get; private set; }

    private LeavePolicy() { }

    public static LeavePolicy Create(
        Guid tenantId,
        Guid leaveTypeId,
        string name,
        int daysPerYear,
        bool allowCarryOver = false,
        int maxCarryOverDays = 0,
        int? maxConsecutiveDays = null,
        int? minNoticeDays = null)
    {
        return new LeavePolicy
        {
            TenantId = tenantId,
            LeaveTypeId = leaveTypeId,
            Name = name,
            DaysPerYear = daysPerYear,
            AllowCarryOver = allowCarryOver,
            MaxCarryOverDays = maxCarryOverDays,
            MaxConsecutiveDays = maxConsecutiveDays,
            MinNoticeDays = minNoticeDays,
            IsActive = true,
        };
    }

    public void Update(string name, int daysPerYear, bool allowCarryOver, int maxCarryOverDays,
        int? maxConsecutiveDays, int? minNoticeDays)
    {
        Name = name;
        DaysPerYear = daysPerYear;
        AllowCarryOver = allowCarryOver;
        MaxCarryOverDays = maxCarryOverDays;
        MaxConsecutiveDays = maxConsecutiveDays;
        MinNoticeDays = minNoticeDays;
    }

    public void Activate() => IsActive = true;
    public void Deactivate() => IsActive = false;
}

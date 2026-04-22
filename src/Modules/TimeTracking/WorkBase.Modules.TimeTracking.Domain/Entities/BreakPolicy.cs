using WorkBase.Shared.Domain;

namespace WorkBase.Modules.TimeTracking.Domain.Entities;

public sealed class BreakPolicy : AuditableEntity<Guid>, ITenantScoped, IAuditable
{
    public Guid TenantId { get; private set; }
    public string Name { get; private set; } = default!;
    public BreakType BreakType { get; private set; }
    public int? MaxPerDay { get; private set; }
    public int? MaxMinutesPerBreak { get; private set; }
    public int? MaxMinutesPerDay { get; private set; }
    public bool IsActive { get; private set; }

    private BreakPolicy() { }

    public static BreakPolicy Create(
        Guid tenantId,
        string name,
        BreakType breakType,
        int? maxPerDay,
        int? maxMinutesPerBreak,
        int? maxMinutesPerDay,
        bool isActive = true)
    {
        return new BreakPolicy
        {
            TenantId = tenantId,
            Name = name,
            BreakType = breakType,
            MaxPerDay = maxPerDay,
            MaxMinutesPerBreak = maxMinutesPerBreak,
            MaxMinutesPerDay = maxMinutesPerDay,
            IsActive = isActive,
        };
    }

    public void Update(string name, int? maxPerDay, int? maxMinutesPerBreak, int? maxMinutesPerDay, bool isActive)
    {
        Name = name;
        MaxPerDay = maxPerDay;
        MaxMinutesPerBreak = maxMinutesPerBreak;
        MaxMinutesPerDay = maxMinutesPerDay;
        IsActive = isActive;
    }
}

public enum BreakType
{
    Paid,
    Unpaid,
}

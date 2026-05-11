using WorkBase.Shared.Domain;

namespace WorkBase.Modules.TimeTracking.Domain.Entities;

public sealed class OrgUnitSchedule : AuditableEntity<Guid>, ITenantScoped, IAuditable
{
    public Guid TenantId { get; private set; }
    public Guid OrgUnitId { get; private set; }
    public string Name { get; private set; } = null!;
    public string WeekPattern { get; private set; } = null!; // JSON array of DayShiftPattern
    public DateOnly EffectiveFrom { get; private set; }
    public bool IsActive { get; private set; }

    private OrgUnitSchedule() { }

    public static OrgUnitSchedule Create(
        Guid tenantId,
        Guid orgUnitId,
        string name,
        string weekPattern,
        DateOnly effectiveFrom)
    {
        return new OrgUnitSchedule
        {
            TenantId = tenantId,
            OrgUnitId = orgUnitId,
            Name = name,
            WeekPattern = weekPattern,
            EffectiveFrom = effectiveFrom,
            IsActive = true
        };
    }

    public void Update(string name, string weekPattern, DateOnly effectiveFrom)
    {
        Name = name;
        WeekPattern = weekPattern;
        EffectiveFrom = effectiveFrom;
    }

    public void Deactivate() => IsActive = false;
    public void Activate() => IsActive = true;
}

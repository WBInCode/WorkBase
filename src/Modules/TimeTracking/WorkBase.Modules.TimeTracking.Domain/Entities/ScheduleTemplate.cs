using WorkBase.Shared.Domain;

namespace WorkBase.Modules.TimeTracking.Domain.Entities;

public sealed class ScheduleTemplate : AuditableEntity<Guid>, ITenantScoped, IAuditable
{
    public Guid TenantId { get; private set; }
    public string Name { get; private set; } = null!;
    public string? Description { get; private set; }
    public string Definition { get; private set; } = null!;
    public bool IsActive { get; private set; }

    private ScheduleTemplate() { }

    public static ScheduleTemplate Create(
        Guid tenantId,
        string name,
        string definition,
        string? description = null)
    {
        return new ScheduleTemplate
        {
            TenantId = tenantId,
            Name = name,
            Definition = definition,
            Description = description,
            IsActive = true
        };
    }

    public void Update(string name, string definition, string? description)
    {
        Name = name;
        Definition = definition;
        Description = description;
    }

    public void Deactivate() => IsActive = false;
    public void Activate() => IsActive = true;
}

using WorkBase.Shared.Domain;

namespace WorkBase.Modules.Organization.Domain.Entities;

public sealed class Tenant : AuditableEntity<Guid>, IAuditable
{
    public string Name { get; private set; } = null!;
    public string Slug { get; private set; } = null!;
    public bool IsActive { get; private set; }
    public string? Settings { get; private set; }

    private Tenant() { }

    public static Tenant Create(string name, string slug)
    {
        return new Tenant
        {
            Name = name,
            Slug = slug,
            IsActive = true
        };
    }

    public void Update(string name)
    {
        Name = name;
    }

    public void Deactivate()
    {
        IsActive = false;
    }

    public void Activate()
    {
        IsActive = true;
    }

    public void UpdateSettings(string? settings)
    {
        Settings = settings;
    }
}

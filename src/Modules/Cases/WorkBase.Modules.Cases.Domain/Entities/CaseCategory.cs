using WorkBase.Shared.Domain;

namespace WorkBase.Modules.Cases.Domain.Entities;

public sealed class CaseCategory : AuditableEntity<Guid>, ITenantScoped, IAuditable
{
    public Guid TenantId { get; private set; }
    public string Name { get; private set; } = null!;
    public string? Description { get; private set; }
    public TimeSpan? DefaultSla { get; private set; }

    private CaseCategory() { }

    public static CaseCategory Create(Guid tenantId, string name, string? description = null, TimeSpan? defaultSla = null)
        => new() { TenantId = tenantId, Name = name, Description = description, DefaultSla = defaultSla };

    public void Update(string name, string? description, TimeSpan? defaultSla)
    {
        Name = name;
        Description = description;
        DefaultSla = defaultSla;
    }
}

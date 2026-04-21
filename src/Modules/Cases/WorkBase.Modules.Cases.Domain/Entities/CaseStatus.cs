using WorkBase.Shared.Domain;

namespace WorkBase.Modules.Cases.Domain.Entities;

public sealed class CaseStatus : AuditableEntity<Guid>, ITenantScoped, IAuditable
{
    public Guid TenantId { get; private set; }
    public string Name { get; private set; } = null!;
    public string Color { get; private set; } = "#6b7280";
    public bool IsDefault { get; private set; }
    public bool IsFinal { get; private set; }
    public int SortOrder { get; private set; }

    private CaseStatus() { }

    public static CaseStatus Create(Guid tenantId, string name, string color, bool isDefault, bool isFinal, int sortOrder)
        => new()
        {
            TenantId = tenantId,
            Name = name,
            Color = color,
            IsDefault = isDefault,
            IsFinal = isFinal,
            SortOrder = sortOrder,
        };

    public void Update(string name, string color, bool isFinal, int sortOrder)
    {
        Name = name;
        Color = color;
        IsFinal = isFinal;
        SortOrder = sortOrder;
    }
}

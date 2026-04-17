using WorkBase.Shared.Domain;

namespace WorkBase.Modules.Tasks.Domain.Entities;

public sealed class TaskStatus : AuditableEntity<Guid>, ITenantScoped, IAuditable
{
    public Guid TenantId { get; private set; }
    public string Code { get; private set; } = null!;
    public string Name { get; private set; } = null!;
    public string? Color { get; private set; }
    public bool IsFinal { get; private set; }
    public bool IsDefault { get; private set; }
    public int SortOrder { get; private set; }
    public bool IsActive { get; private set; }

    private TaskStatus() { }

    public static TaskStatus Create(
        Guid tenantId, string code, string name,
        bool isFinal = false, bool isDefault = false,
        string? color = null, int sortOrder = 0)
    {
        return new TaskStatus
        {
            TenantId = tenantId,
            Code = code,
            Name = name,
            IsFinal = isFinal,
            IsDefault = isDefault,
            Color = color,
            SortOrder = sortOrder,
            IsActive = true,
        };
    }

    public void Update(string name, string? color, bool isFinal, int sortOrder)
    {
        Name = name;
        Color = color;
        IsFinal = isFinal;
        SortOrder = sortOrder;
    }

    public void Activate() => IsActive = true;
    public void Deactivate() => IsActive = false;
}

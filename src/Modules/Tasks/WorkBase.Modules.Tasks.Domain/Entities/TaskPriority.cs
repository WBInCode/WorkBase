using WorkBase.Shared.Domain;

namespace WorkBase.Modules.Tasks.Domain.Entities;

public sealed class TaskPriority : AuditableEntity<Guid>, ITenantScoped, IAuditable
{
    public Guid TenantId { get; private set; }
    public string Code { get; private set; } = null!;
    public string Name { get; private set; } = null!;
    public string? Color { get; private set; }
    public int SortOrder { get; private set; }
    public bool IsActive { get; private set; }

    private TaskPriority() { }

    public static TaskPriority Create(
        Guid tenantId, string code, string name,
        string? color = null, int sortOrder = 0)
    {
        return new TaskPriority
        {
            TenantId = tenantId,
            Code = code,
            Name = name,
            Color = color,
            SortOrder = sortOrder,
            IsActive = true,
        };
    }

    public void Update(string name, string? color, int sortOrder)
    {
        Name = name;
        Color = color;
        SortOrder = sortOrder;
    }

    public void Activate() => IsActive = true;
    public void Deactivate() => IsActive = false;
}

using WorkBase.Shared.Domain;

namespace WorkBase.Modules.Leave.Domain.Entities;

/// <summary>
/// Typ nieobecności (urlop wypoczynkowy, na żądanie, L4, opieka, etc.).
/// Edytowalny słownik per tenant.
/// </summary>
public sealed class LeaveType : AuditableEntity<Guid>, ITenantScoped, IAuditable
{
    public Guid TenantId { get; private set; }
    public string Code { get; private set; } = null!;
    public string Name { get; private set; } = null!;
    public string? Description { get; private set; }
    public bool IsPaid { get; private set; }
    public bool RequiresApproval { get; private set; }
    public int? DefaultDaysPerYear { get; private set; }
    public string? Color { get; private set; }
    public bool IsActive { get; private set; }
    public int SortOrder { get; private set; }

    private LeaveType() { }

    public static LeaveType Create(
        Guid tenantId,
        string code,
        string name,
        bool isPaid,
        bool requiresApproval,
        int? defaultDaysPerYear = null,
        string? description = null,
        string? color = null,
        int sortOrder = 0)
    {
        return new LeaveType
        {
            TenantId = tenantId,
            Code = code,
            Name = name,
            IsPaid = isPaid,
            RequiresApproval = requiresApproval,
            DefaultDaysPerYear = defaultDaysPerYear,
            Description = description,
            Color = color,
            IsActive = true,
            SortOrder = sortOrder,
        };
    }

    public void Update(string name, string? description, bool isPaid, bool requiresApproval,
        int? defaultDaysPerYear, string? color, int sortOrder)
    {
        Name = name;
        Description = description;
        IsPaid = isPaid;
        RequiresApproval = requiresApproval;
        DefaultDaysPerYear = defaultDaysPerYear;
        Color = color;
        SortOrder = sortOrder;
    }

    public void Activate() => IsActive = true;
    public void Deactivate() => IsActive = false;
}

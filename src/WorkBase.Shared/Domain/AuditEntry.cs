namespace WorkBase.Shared.Domain;

public sealed class AuditEntry
{
    public Guid Id { get; private set; }
    public string EntityType { get; private set; } = default!;
    public string EntityId { get; private set; } = default!;
    public string Action { get; private set; } = default!;
    public string? OldValues { get; private set; }
    public string? NewValues { get; private set; }
    public string? ChangedColumns { get; private set; }
    public Guid? TenantId { get; private set; }
    public string? UserId { get; private set; }
    public DateTime Timestamp { get; private set; }

    private AuditEntry() { }

    public static AuditEntry Create(
        string entityType,
        string entityId,
        string action,
        string? oldValues,
        string? newValues,
        string? changedColumns,
        Guid? tenantId,
        string? userId)
    {
        return new AuditEntry
        {
            EntityType = entityType,
            EntityId = entityId,
            Action = action,
            OldValues = oldValues,
            NewValues = newValues,
            ChangedColumns = changedColumns,
            TenantId = tenantId,
            UserId = userId,
            Timestamp = DateTime.UtcNow
        };
    }
}

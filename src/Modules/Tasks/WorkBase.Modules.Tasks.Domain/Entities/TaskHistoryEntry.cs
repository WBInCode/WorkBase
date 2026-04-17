using WorkBase.Shared.Domain;

namespace WorkBase.Modules.Tasks.Domain.Entities;

public sealed class TaskHistoryEntry : Entity<Guid>, ITenantScoped
{
    public Guid TenantId { get; private set; }
    public Guid TaskId { get; private set; }
    public Guid ChangedById { get; private set; }
    public string FieldName { get; private set; } = null!;
    public string? OldValue { get; private set; }
    public string? NewValue { get; private set; }
    public DateTime ChangedAt { get; private set; }

    private TaskHistoryEntry() { }

    public static TaskHistoryEntry Create(
        Guid tenantId, Guid taskId, Guid changedById,
        string fieldName, string? oldValue, string? newValue)
    {
        return new TaskHistoryEntry
        {
            TenantId = tenantId,
            TaskId = taskId,
            ChangedById = changedById,
            FieldName = fieldName,
            OldValue = oldValue,
            NewValue = newValue,
            ChangedAt = DateTime.UtcNow,
        };
    }
}

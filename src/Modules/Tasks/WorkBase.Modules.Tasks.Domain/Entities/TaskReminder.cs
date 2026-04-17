using WorkBase.Shared.Domain;

namespace WorkBase.Modules.Tasks.Domain.Entities;

public sealed class TaskReminder : Entity<Guid>, ITenantScoped
{
    public Guid TenantId { get; private set; }
    public Guid TaskId { get; private set; }
    public Guid RecipientId { get; private set; }
    public DateTime RemindAt { get; private set; }
    public bool IsSent { get; private set; }
    public DateTime? SentAt { get; private set; }

    private TaskReminder() { }

    public static TaskReminder Create(
        Guid tenantId, Guid taskId, Guid recipientId, DateTime remindAt)
    {
        return new TaskReminder
        {
            TenantId = tenantId,
            TaskId = taskId,
            RecipientId = recipientId,
            RemindAt = remindAt,
            IsSent = false,
        };
    }

    public void MarkSent()
    {
        IsSent = true;
        SentAt = DateTime.UtcNow;
    }
}

using WorkBase.Shared.Domain;

namespace WorkBase.Modules.Tasks.Domain.Entities;

public sealed class TaskComment : AuditableEntity<Guid>, ITenantScoped, IAuditable
{
    public Guid TenantId { get; private set; }
    public Guid TaskId { get; private set; }
    public Guid AuthorId { get; private set; }
    public string Content { get; private set; } = null!;

    private TaskComment() { }

    public static TaskComment Create(
        Guid tenantId, Guid taskId, Guid authorId, string content)
    {
        return new TaskComment
        {
            TenantId = tenantId,
            TaskId = taskId,
            AuthorId = authorId,
            Content = content,
        };
    }

    public void UpdateContent(string content)
    {
        Content = content;
    }
}

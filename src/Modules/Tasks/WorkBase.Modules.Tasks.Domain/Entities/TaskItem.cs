using WorkBase.Shared.Domain;

namespace WorkBase.Modules.Tasks.Domain.Entities;

public sealed class TaskItem : AuditableEntity<Guid>, ITenantScoped, IAuditable
{
    public Guid TenantId { get; private set; }
    public string Title { get; private set; } = null!;
    public string? Description { get; private set; }
    public Guid StatusId { get; private set; }
    public Guid PriorityId { get; private set; }
    public Guid AssigneeId { get; private set; }
    public Guid? ReporterId { get; private set; }
    public DateTime? DueDate { get; private set; }
    public DateTime? CompletedAt { get; private set; }
    public Guid? WorkflowInstanceId { get; private set; }

    private TaskItem() { }

    public static TaskItem Create(
        Guid tenantId, string title, Guid statusId, Guid priorityId,
        Guid assigneeId, Guid? reporterId = null, string? description = null,
        DateTime? dueDate = null)
    {
        return new TaskItem
        {
            TenantId = tenantId,
            Title = title,
            StatusId = statusId,
            PriorityId = priorityId,
            AssigneeId = assigneeId,
            ReporterId = reporterId,
            Description = description,
            DueDate = dueDate,
        };
    }

    public void Update(string title, string? description, Guid statusId,
        Guid priorityId, Guid assigneeId, DateTime? dueDate)
    {
        Title = title;
        Description = description;
        StatusId = statusId;
        PriorityId = priorityId;
        AssigneeId = assigneeId;
        DueDate = dueDate;
    }

    public void ChangeStatus(Guid newStatusId)
    {
        StatusId = newStatusId;
    }

    public void Complete(DateTime completedAt)
    {
        CompletedAt = completedAt;
    }

    public void LinkWorkflow(Guid workflowInstanceId)
    {
        WorkflowInstanceId = workflowInstanceId;
    }
}

using WorkBase.Modules.Tasks.Domain.Events;
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
        var task = new TaskItem
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

        task.RaiseDomainEvent(new TaskCreatedEvent(
            task.Id, tenantId, assigneeId, title, statusId, priorityId));

        return task;
    }

    public void Update(string title, string? description,
        Guid priorityId, DateTime? dueDate)
    {
        Title = title;
        Description = description;
        PriorityId = priorityId;
        DueDate = dueDate;
    }

    public void ChangeStatus(Guid newStatusId, Guid changedById)
    {
        var oldStatusId = StatusId;
        StatusId = newStatusId;
        RaiseDomainEvent(new TaskStatusChangedEvent(
            Id, TenantId, oldStatusId, newStatusId, changedById));
    }

    public void Assign(Guid newAssigneeId)
    {
        var oldAssigneeId = AssigneeId;
        AssigneeId = newAssigneeId;
        RaiseDomainEvent(new TaskAssignedEvent(
            Id, TenantId, oldAssigneeId, newAssigneeId));
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

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

    private readonly List<TaskAssignee> _additionalAssignees = new();
    public IReadOnlyCollection<TaskAssignee> AdditionalAssignees => _additionalAssignees.AsReadOnly();

    private TaskItem() { }

    public static TaskItem Create(
        Guid tenantId, string title, Guid statusId, Guid priorityId,
        Guid assigneeId, Guid? reporterId = null, string? description = null,
        DateTime? dueDate = null, IEnumerable<Guid>? additionalAssigneeIds = null)
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

        if (additionalAssigneeIds is not null)
        {
            foreach (var employeeId in additionalAssigneeIds.Distinct())
            {
                if (employeeId == Guid.Empty || employeeId == assigneeId) continue;
                task._additionalAssignees.Add(TaskAssignee.Create(task.Id, employeeId));
            }
        }

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
        _additionalAssignees.RemoveAll(a => a.EmployeeId == newAssigneeId);
        RaiseDomainEvent(new TaskAssignedEvent(
            Id, TenantId, oldAssigneeId, newAssigneeId));
    }

    public void SetAdditionalAssignees(IEnumerable<Guid> employeeIds)
    {
        var distinct = employeeIds
            .Where(id => id != Guid.Empty && id != AssigneeId)
            .Distinct()
            .ToHashSet();

        _additionalAssignees.RemoveAll(a => !distinct.Contains(a.EmployeeId));
        var existing = _additionalAssignees.Select(a => a.EmployeeId).ToHashSet();
        foreach (var id in distinct)
        {
            if (!existing.Contains(id))
                _additionalAssignees.Add(TaskAssignee.Create(Id, id));
        }
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

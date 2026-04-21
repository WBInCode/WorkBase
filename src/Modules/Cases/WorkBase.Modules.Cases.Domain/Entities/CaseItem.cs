using WorkBase.Modules.Cases.Domain.Events;
using WorkBase.Shared.Domain;

namespace WorkBase.Modules.Cases.Domain.Entities;

public sealed class CaseItem : AuditableEntity<Guid>, ITenantScoped, IAuditable
{
    public Guid TenantId { get; private set; }
    public string Number { get; private set; } = null!;
    public string Title { get; private set; } = null!;
    public string? Description { get; private set; }
    public Guid StatusId { get; private set; }
    public Guid? CategoryId { get; private set; }
    public Guid? AssigneeId { get; private set; }
    public Guid? ContactId { get; private set; }
    public CasePriorityLevel Priority { get; private set; }
    public DateTime? DueDate { get; private set; }
    public DateTime? ResolvedAt { get; private set; }
    public DateTime? ClosedAt { get; private set; }
    public Guid? WorkflowInstanceId { get; private set; }

    private CaseItem() { }

    public static CaseItem Create(
        Guid tenantId, string number, string title, Guid statusId,
        CasePriorityLevel priority, Guid? categoryId = null,
        Guid? assigneeId = null, Guid? contactId = null,
        string? description = null, DateTime? dueDate = null)
    {
        var item = new CaseItem
        {
            TenantId = tenantId,
            Number = number,
            Title = title,
            StatusId = statusId,
            Priority = priority,
            CategoryId = categoryId,
            AssigneeId = assigneeId,
            ContactId = contactId,
            Description = description,
            DueDate = dueDate,
        };
        item.RaiseDomainEvent(new CaseCreatedEvent(item.Id, tenantId, number, title, priority));
        return item;
    }

    public void Update(string title, string? description, CasePriorityLevel priority, DateTime? dueDate, Guid? categoryId)
    {
        Title = title;
        Description = description;
        Priority = priority;
        DueDate = dueDate;
        CategoryId = categoryId;
    }

    public void ChangeStatus(Guid newStatusId, Guid changedById)
    {
        var old = StatusId;
        StatusId = newStatusId;
        RaiseDomainEvent(new CaseStatusChangedEvent(Id, TenantId, old, newStatusId, changedById));
    }

    public void Assign(Guid? assigneeId)
    {
        AssigneeId = assigneeId;
        RaiseDomainEvent(new CaseAssignedEvent(Id, TenantId, assigneeId));
    }

    public void Resolve(DateTime resolvedAt)
    {
        ResolvedAt = resolvedAt;
        RaiseDomainEvent(new CaseResolvedEvent(Id, TenantId, Number));
    }

    public void Close(DateTime closedAt)
    {
        ClosedAt = closedAt;
    }

    public void LinkWorkflow(Guid workflowInstanceId) => WorkflowInstanceId = workflowInstanceId;

    public bool IsSlaBreached() => DueDate.HasValue && !ResolvedAt.HasValue && DateTime.UtcNow > DueDate.Value;
}

public enum CasePriorityLevel
{
    Low = 0,
    Normal = 1,
    High = 2,
    Critical = 3,
}

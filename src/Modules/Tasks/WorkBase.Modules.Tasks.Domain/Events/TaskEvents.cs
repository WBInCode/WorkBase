using WorkBase.Shared.Domain;

namespace WorkBase.Modules.Tasks.Domain.Events;

public sealed record TaskCreatedEvent(
    Guid TaskId, Guid TenantId, Guid AssigneeId,
    string Title, Guid StatusId, Guid PriorityId) : DomainEvent;

public sealed record TaskStatusChangedEvent(
    Guid TaskId, Guid TenantId,
    Guid OldStatusId, Guid NewStatusId, Guid ChangedById) : DomainEvent;

public sealed record TaskAssignedEvent(
    Guid TaskId, Guid TenantId,
    Guid OldAssigneeId, Guid NewAssigneeId) : DomainEvent;

public sealed record TaskCommentAddedEvent(
    Guid CommentId, Guid TaskId, Guid TenantId,
    Guid AuthorId) : DomainEvent;

public sealed record TaskOverdueEvent(
    Guid TaskId, Guid TenantId, Guid AssigneeId,
    string Title, DateTime DueDate) : DomainEvent;

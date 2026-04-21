using WorkBase.Modules.Cases.Domain.Entities;
using WorkBase.Shared.Domain;

namespace WorkBase.Modules.Cases.Domain.Events;

public sealed record CaseCreatedEvent(
    Guid CaseId, Guid TenantId, string Number, string Title,
    CasePriorityLevel Priority) : DomainEvent;

public sealed record CaseStatusChangedEvent(
    Guid CaseId, Guid TenantId,
    Guid OldStatusId, Guid NewStatusId, Guid ChangedById) : DomainEvent;

public sealed record CaseAssignedEvent(
    Guid CaseId, Guid TenantId, Guid? AssigneeId) : DomainEvent;

public sealed record CaseResolvedEvent(
    Guid CaseId, Guid TenantId, string Number) : DomainEvent;

public sealed record CaseSlaBreachedEvent(
    Guid CaseId, Guid TenantId, string Number, DateTime DueDate) : DomainEvent;

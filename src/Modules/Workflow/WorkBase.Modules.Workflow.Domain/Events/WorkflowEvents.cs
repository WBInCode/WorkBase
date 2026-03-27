using WorkBase.Shared.Domain;

namespace WorkBase.Modules.Workflow.Domain.Events;

public sealed record WorkflowInstanceCreatedEvent(
    Guid InstanceId,
    Guid TenantId,
    Guid DefinitionId,
    string EntityType,
    Guid EntityId,
    string InitialStep) : DomainEvent;

public sealed record WorkflowStepCompletedEvent(
    Guid InstanceId,
    Guid StepId,
    Guid TenantId,
    string StepName,
    string Outcome,
    string EntityType,
    Guid EntityId) : DomainEvent;

public sealed record WorkflowInstanceCompletedEvent(
    Guid InstanceId,
    Guid TenantId,
    string EntityType,
    Guid EntityId,
    string FinalStepName) : DomainEvent;

public sealed record WorkflowInstanceRejectedEvent(
    Guid InstanceId,
    Guid TenantId,
    string EntityType,
    Guid EntityId,
    string StepName) : DomainEvent;

public sealed record WorkflowStepAdvancedEvent(
    Guid InstanceId,
    Guid TenantId,
    string FromStep,
    string ToStep,
    string EntityType,
    Guid EntityId) : DomainEvent;

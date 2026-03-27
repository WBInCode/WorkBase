namespace WorkBase.Modules.Workflow.Application.Dtos;

public sealed record WorkflowDefinitionDto(
    Guid Id,
    string Name,
    string? Description,
    string DefinitionJson,
    int Version,
    bool IsActive,
    DateTime CreatedAt);

public sealed record WorkflowInstanceDto(
    Guid Id,
    Guid DefinitionId,
    string DefinitionName,
    string EntityType,
    Guid EntityId,
    string CurrentStepName,
    string Status,
    Guid InitiatedBy,
    DateTime CreatedAt,
    DateTime? CompletedAt);

public sealed record WorkflowStepDto(
    Guid Id,
    string StepName,
    string Status,
    DateTime? EnteredAt,
    DateTime? CompletedAt,
    string? CompletedBy,
    string? Outcome,
    string? Comment);

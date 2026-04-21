namespace WorkBase.Modules.Forms.Application.Dtos;

public sealed record FormDefinitionDto(
    Guid Id, string Name, string? Description, int Version,
    bool IsActive, bool IsPublic, string? WorkflowDefinitionName,
    DateTime CreatedAt, List<FormFieldDto> Fields);

public sealed record FormFieldDto(
    Guid Id, string Label, string FieldType, int Order,
    bool IsRequired, string? Placeholder, string? ValidationRule,
    string? OptionsJson, string? DefaultValue);

public sealed record FormSubmissionDto(
    Guid Id, Guid FormDefinitionId, string FormName,
    Guid? SubmittedBy, string ValuesJson, string Status,
    Guid? WorkflowInstanceId, DateTime CreatedAt);

public sealed record CreateFormDefinitionRequest(
    string Name, string? Description, bool IsPublic,
    string? WorkflowDefinitionName, List<CreateFormFieldRequest> Fields);

public sealed record CreateFormFieldRequest(
    string Label, string FieldType, int Order, bool IsRequired,
    string? Placeholder, string? ValidationRule,
    string? OptionsJson, string? DefaultValue);

public sealed record UpdateFormDefinitionRequest(
    string Name, string? Description, bool IsPublic,
    string? WorkflowDefinitionName, List<CreateFormFieldRequest> Fields);

public sealed record SubmitFormRequest(Guid FormDefinitionId, string ValuesJson);

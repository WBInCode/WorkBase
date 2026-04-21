using WorkBase.Modules.Forms.Application.Contracts;
using WorkBase.Modules.Forms.Application.Dtos;
using WorkBase.Modules.Forms.Domain.Entities;
using WorkBase.Shared.Cqrs;
using WorkBase.Shared.Domain;

namespace WorkBase.Modules.Forms.Application.Commands;

// --- Create Definition ---
public sealed record CreateFormDefinitionCommand(
    string Name, string? Description, bool IsPublic,
    string? WorkflowDefinitionName, List<CreateFormFieldRequest> Fields) : ICommand<Guid>, ITenantRequest
{
    public Guid TenantId { get; set; }
}

public sealed class CreateFormDefinitionHandler(
    IFormDefinitionRepository definitionRepo) : ICommandHandler<CreateFormDefinitionCommand, Guid>
{
    public async Task<Result<Guid>> Handle(CreateFormDefinitionCommand cmd, CancellationToken ct)
    {
        var def = FormDefinition.Create(cmd.TenantId, cmd.Name, cmd.Description, cmd.IsPublic, cmd.WorkflowDefinitionName);

        foreach (var f in cmd.Fields.OrderBy(f => f.Order))
            def.AddField(f.Label, f.FieldType, f.Order, f.IsRequired, f.Placeholder, f.ValidationRule, f.OptionsJson, f.DefaultValue);

        await definitionRepo.AddAsync(def, ct);
        return def.Id;
    }
}

// --- Update Definition ---
public sealed record UpdateFormDefinitionCommand(
    Guid Id, string Name, string? Description, bool IsPublic,
    string? WorkflowDefinitionName, List<CreateFormFieldRequest> Fields) : ICommand, ITenantRequest
{
    public Guid TenantId { get; set; }
}

public sealed class UpdateFormDefinitionHandler(
    IFormDefinitionRepository definitionRepo,
    IFormFieldRepository fieldRepo) : ICommandHandler<UpdateFormDefinitionCommand>
{
    public async Task<Result> Handle(UpdateFormDefinitionCommand cmd, CancellationToken ct)
    {
        var def = await definitionRepo.GetByIdWithFieldsAsync(cmd.Id, ct);
        if (def is null) return Result.Failure(Error.NotFound("Forms.NotFound", "Formularz nie został znaleziony."));

        def.Update(cmd.Name, cmd.Description, cmd.IsPublic, cmd.WorkflowDefinitionName);

        // Replace fields
        var existing = await fieldRepo.GetByDefinitionAsync(cmd.Id, ct);
        fieldRepo.RemoveRange(existing);

        foreach (var f in cmd.Fields.OrderBy(f => f.Order))
            def.AddField(f.Label, f.FieldType, f.Order, f.IsRequired, f.Placeholder, f.ValidationRule, f.OptionsJson, f.DefaultValue);

        definitionRepo.Update(def);
        return Result.Success();
    }
}

// --- Delete Definition ---
public sealed record DeleteFormDefinitionCommand(Guid Id) : ICommand, ITenantRequest
{
    public Guid TenantId { get; set; }
}

public sealed class DeleteFormDefinitionHandler(
    IFormDefinitionRepository repo) : ICommandHandler<DeleteFormDefinitionCommand>
{
    public async Task<Result> Handle(DeleteFormDefinitionCommand cmd, CancellationToken ct)
    {
        var def = await repo.GetByIdAsync(cmd.Id, ct);
        if (def is null) return Result.Failure(Error.NotFound("Forms.NotFound", "Formularz nie został znaleziony."));
        repo.Remove(def);
        return Result.Success();
    }
}

// --- Submit Form ---
public sealed record SubmitFormCommand(Guid FormDefinitionId, string ValuesJson) : ICommand<Guid>, ITenantRequest
{
    public Guid TenantId { get; set; }
    public Guid? SubmittedBy { get; set; }
}

public sealed class SubmitFormHandler(
    IFormDefinitionRepository definitionRepo,
    IFormSubmissionRepository submissionRepo) : ICommandHandler<SubmitFormCommand, Guid>
{
    public async Task<Result<Guid>> Handle(SubmitFormCommand cmd, CancellationToken ct)
    {
        var def = await definitionRepo.GetByIdAsync(cmd.FormDefinitionId, ct);
        if (def is null) return Result.Failure<Guid>(Error.NotFound("Forms.NotFound", "Formularz nie został znaleziony."));
        if (!def.IsActive) return Result.Failure<Guid>(new Error("Forms.Inactive", "Formularz jest nieaktywny."));

        var submission = FormSubmission.Create(cmd.TenantId, cmd.FormDefinitionId, cmd.SubmittedBy, cmd.ValuesJson);
        submission.Submit();

        await submissionRepo.AddAsync(submission, ct);
        return submission.Id;
    }
}

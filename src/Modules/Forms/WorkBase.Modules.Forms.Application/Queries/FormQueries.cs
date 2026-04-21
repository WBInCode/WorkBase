using WorkBase.Modules.Forms.Application.Contracts;
using WorkBase.Modules.Forms.Application.Dtos;
using WorkBase.Shared.Cqrs;
using WorkBase.Shared.Domain;

namespace WorkBase.Modules.Forms.Application.Queries;

// --- Get All Definitions ---
public sealed record GetFormDefinitionsQuery : IQuery<List<FormDefinitionDto>>, ITenantRequest
{
    public Guid TenantId { get; set; }
}

public sealed class GetFormDefinitionsHandler(
    IFormDefinitionRepository repo,
    IFormFieldRepository fieldRepo) : IQueryHandler<GetFormDefinitionsQuery, List<FormDefinitionDto>>
{
    public async Task<Result<List<FormDefinitionDto>>> Handle(GetFormDefinitionsQuery query, CancellationToken ct)
    {
        var defs = await repo.GetAllAsync(query.TenantId, ct);
        var result = new List<FormDefinitionDto>();

        foreach (var d in defs)
        {
            var fields = await fieldRepo.GetByDefinitionAsync(d.Id, ct);
            result.Add(new FormDefinitionDto(
                d.Id, d.Name, d.Description, d.Version,
                d.IsActive, d.IsPublic, d.WorkflowDefinitionName, d.CreatedAt,
                fields.OrderBy(f => f.Order).Select(f => new FormFieldDto(
                    f.Id, f.Label, f.FieldType, f.Order, f.IsRequired,
                    f.Placeholder, f.ValidationRule, f.OptionsJson, f.DefaultValue
                )).ToList()));
        }

        return result;
    }
}

// --- Get Definition By Id ---
public sealed record GetFormDefinitionByIdQuery(Guid Id) : IQuery<FormDefinitionDto>, ITenantRequest
{
    public Guid TenantId { get; set; }
}

public sealed class GetFormDefinitionByIdHandler(
    IFormDefinitionRepository repo,
    IFormFieldRepository fieldRepo) : IQueryHandler<GetFormDefinitionByIdQuery, FormDefinitionDto>
{
    public async Task<Result<FormDefinitionDto>> Handle(GetFormDefinitionByIdQuery query, CancellationToken ct)
    {
        var d = await repo.GetByIdAsync(query.Id, ct);
        if (d is null) return Result.Failure<FormDefinitionDto>(Error.NotFound("Forms.NotFound", "Formularz nie został znaleziony."));

        var fields = await fieldRepo.GetByDefinitionAsync(d.Id, ct);
        return new FormDefinitionDto(
            d.Id, d.Name, d.Description, d.Version,
            d.IsActive, d.IsPublic, d.WorkflowDefinitionName, d.CreatedAt,
            fields.OrderBy(f => f.Order).Select(f => new FormFieldDto(
                f.Id, f.Label, f.FieldType, f.Order, f.IsRequired,
                f.Placeholder, f.ValidationRule, f.OptionsJson, f.DefaultValue
            )).ToList());
    }
}

// --- Get Submissions ---
public sealed record GetFormSubmissionsQuery(Guid FormDefinitionId) : IQuery<List<FormSubmissionDto>>, ITenantRequest
{
    public Guid TenantId { get; set; }
}

public sealed class GetFormSubmissionsHandler(
    IFormSubmissionRepository repo,
    IFormDefinitionRepository defRepo) : IQueryHandler<GetFormSubmissionsQuery, List<FormSubmissionDto>>
{
    public async Task<Result<List<FormSubmissionDto>>> Handle(GetFormSubmissionsQuery query, CancellationToken ct)
    {
        var def = await defRepo.GetByIdAsync(query.FormDefinitionId, ct);
        var submissions = await repo.GetByDefinitionAsync(query.FormDefinitionId, ct);
        return submissions.Select(s => new FormSubmissionDto(
            s.Id, s.FormDefinitionId, def?.Name ?? "",
            s.SubmittedBy, s.ValuesJson, s.Status,
            s.WorkflowInstanceId, s.CreatedAt
        )).ToList();
    }
}

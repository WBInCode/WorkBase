using WorkBase.Modules.Cases.Application.Contracts;
using WorkBase.Modules.Cases.Application.Dtos;
using WorkBase.Modules.Cases.Domain.Entities;
using WorkBase.Shared.Cqrs;
using WorkBase.Shared.Domain;

namespace WorkBase.Modules.Cases.Application.Queries;

public sealed record GetCasesQuery(Guid? AssigneeId = null, Guid? ContactId = null) : IQuery<List<CaseItemDto>>, ITenantRequest
{
    public Guid TenantId { get; set; }
}

public sealed class GetCasesHandler(
    ICaseItemRepository caseRepo,
    ICaseStatusRepository statusRepo,
    ICaseCategoryRepository categoryRepo)
    : IQueryHandler<GetCasesQuery, List<CaseItemDto>>
{
    public async Task<Result<List<CaseItemDto>>> Handle(GetCasesQuery request, CancellationToken ct)
    {
        var cases = request.AssigneeId.HasValue
            ? await caseRepo.GetByAssigneeAsync(request.TenantId, request.AssigneeId.Value, ct)
            : request.ContactId.HasValue
                ? await caseRepo.GetByContactAsync(request.TenantId, request.ContactId.Value, ct)
                : await caseRepo.GetByTenantAsync(request.TenantId, ct);

        var statuses = await statusRepo.GetByTenantAsync(request.TenantId, ct);
        var categories = await categoryRepo.GetByTenantAsync(request.TenantId, ct);
        var statusMap = statuses.ToDictionary(s => s.Id);
        var categoryMap = categories.ToDictionary(c => c.Id);

        var dtos = cases.Select(c =>
        {
            statusMap.TryGetValue(c.StatusId, out var status);
            CaseCategory? category = c.CategoryId.HasValue && categoryMap.TryGetValue(c.CategoryId.Value, out var cat) ? cat : null;
            return new CaseItemDto(
                c.Id, c.Number, c.Title, c.Description,
                c.StatusId, status?.Name ?? "", status?.Color ?? "#6b7280",
                c.Priority, c.AssigneeId, c.ContactId,
                c.CategoryId, category?.Name,
                c.DueDate, c.ResolvedAt, c.ClosedAt,
                c.IsSlaBreached(), c.CreatedAt, c.ModifiedAt);
        }).ToList();

        return dtos;
    }
}

public sealed record GetCaseByIdQuery(Guid CaseId) : IQuery<CaseItemDto>, ITenantRequest
{
    public Guid TenantId { get; set; }
}

public sealed class GetCaseByIdHandler(
    ICaseItemRepository caseRepo,
    ICaseStatusRepository statusRepo,
    ICaseCategoryRepository categoryRepo)
    : IQueryHandler<GetCaseByIdQuery, CaseItemDto>
{
    public async Task<Result<CaseItemDto>> Handle(GetCaseByIdQuery request, CancellationToken ct)
    {
        var c = await caseRepo.GetByIdAsync(request.CaseId, ct);
        if (c is null) return Result.Failure<CaseItemDto>(Error.NotFound("Case.NotFound", "Sprawa nie została znaleziona."));

        var status = await statusRepo.GetByIdAsync(c.StatusId, ct);
        CaseCategory? category = c.CategoryId.HasValue ? await categoryRepo.GetByIdAsync(c.CategoryId.Value, ct) : null;

        return new CaseItemDto(
            c.Id, c.Number, c.Title, c.Description,
            c.StatusId, status?.Name ?? "", status?.Color ?? "#6b7280",
            c.Priority, c.AssigneeId, c.ContactId,
            c.CategoryId, category?.Name,
            c.DueDate, c.ResolvedAt, c.ClosedAt,
            c.IsSlaBreached(), c.CreatedAt, c.ModifiedAt);
    }
}

public sealed record GetCaseStatusesQuery : IQuery<List<CaseStatusDto>>, ITenantRequest
{
    public Guid TenantId { get; set; }
}

public sealed class GetCaseStatusesHandler(ICaseStatusRepository repo) : IQueryHandler<GetCaseStatusesQuery, List<CaseStatusDto>>
{
    public async Task<Result<List<CaseStatusDto>>> Handle(GetCaseStatusesQuery request, CancellationToken ct)
    {
        var statuses = await repo.GetByTenantAsync(request.TenantId, ct);
        return statuses.Select(s => new CaseStatusDto(s.Id, s.Name, s.Color, s.IsDefault, s.IsFinal, s.SortOrder)).ToList();
    }
}

public sealed record GetCaseCategoriesQuery : IQuery<List<CaseCategoryDto>>, ITenantRequest
{
    public Guid TenantId { get; set; }
}

public sealed class GetCaseCategoriesHandler(ICaseCategoryRepository repo) : IQueryHandler<GetCaseCategoriesQuery, List<CaseCategoryDto>>
{
    public async Task<Result<List<CaseCategoryDto>>> Handle(GetCaseCategoriesQuery request, CancellationToken ct)
    {
        var categories = await repo.GetByTenantAsync(request.TenantId, ct);
        return categories.Select(c => new CaseCategoryDto(c.Id, c.Name, c.Description, c.DefaultSla)).ToList();
    }
}

public sealed record GetCaseCommentsQuery(Guid CaseId) : IQuery<List<CaseCommentDto>>, ITenantRequest
{
    public Guid TenantId { get; set; }
}

public sealed class GetCaseCommentsHandler(ICaseCommentRepository repo) : IQueryHandler<GetCaseCommentsQuery, List<CaseCommentDto>>
{
    public async Task<Result<List<CaseCommentDto>>> Handle(GetCaseCommentsQuery request, CancellationToken ct)
    {
        var comments = await repo.GetByCaseAsync(request.CaseId, ct);
        return comments.Select(c => new CaseCommentDto(c.Id, c.CaseId, c.AuthorId, c.Content, c.IsInternal, c.CreatedAt)).ToList();
    }
}

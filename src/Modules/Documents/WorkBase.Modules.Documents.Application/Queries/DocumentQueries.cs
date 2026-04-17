using WorkBase.Modules.Documents.Application.Contracts;
using WorkBase.Shared.Cqrs;
using WorkBase.Shared.Domain;

namespace WorkBase.Modules.Documents.Application.Queries;

public sealed record DocumentDto(
    Guid Id, string FileName, string ContentType, long FileSizeBytes,
    Guid UploadedById, Guid? CategoryId, string? EntityType, Guid? EntityId,
    string? Description, bool IsDeleted, DateTime CreatedAt);

public sealed record GetDocumentsQuery(
    Guid? CategoryId = null, string? EntityType = null, Guid? EntityId = null, bool IncludeDeleted = false)
    : IQuery<List<DocumentDto>>, ITenantRequest
{
    public Guid TenantId { get; set; }
}

public sealed class GetDocumentsHandler(IDocumentRepository repository)
    : IQueryHandler<GetDocumentsQuery, List<DocumentDto>>
{
    public async Task<Result<List<DocumentDto>>> Handle(GetDocumentsQuery request, CancellationToken cancellationToken)
    {
        var docs = await repository.GetByTenantAsync(
            request.TenantId, request.CategoryId, request.EntityType, request.EntityId,
            request.IncludeDeleted, cancellationToken);

        return docs.Select(d => new DocumentDto(
            d.Id, d.FileName, d.ContentType, d.FileSizeBytes,
            d.UploadedById, d.CategoryId, d.EntityType, d.EntityId,
            d.Description, d.IsDeleted, d.CreatedAt)).ToList();
    }
}

public sealed record DocumentCategoryDto(Guid Id, string Name, string? Description);

public sealed record GetDocumentCategoriesQuery() : IQuery<List<DocumentCategoryDto>>, ITenantRequest
{
    public Guid TenantId { get; set; }
}

public sealed class GetDocumentCategoriesHandler(IDocumentCategoryRepository repository)
    : IQueryHandler<GetDocumentCategoriesQuery, List<DocumentCategoryDto>>
{
    public async Task<Result<List<DocumentCategoryDto>>> Handle(
        GetDocumentCategoriesQuery request, CancellationToken cancellationToken)
    {
        var categories = await repository.GetByTenantAsync(request.TenantId, cancellationToken);
        return categories.Select(c => new DocumentCategoryDto(c.Id, c.Name, c.Description)).ToList();
    }
}

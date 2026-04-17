using WorkBase.Modules.Documents.Application.Contracts;
using WorkBase.Shared.Cqrs;
using WorkBase.Shared.Domain;

namespace WorkBase.Modules.Documents.Application.Queries;

public sealed record GetDocumentAuditByUserQuery(Guid UserId)
    : IQuery<List<DocumentDto>>, ITenantRequest
{
    public Guid TenantId { get; set; }
}

public sealed class GetDocumentAuditByUserHandler(IDocumentRepository repository)
    : IQueryHandler<GetDocumentAuditByUserQuery, List<DocumentDto>>
{
    public async Task<Result<List<DocumentDto>>> Handle(
        GetDocumentAuditByUserQuery request, CancellationToken cancellationToken)
    {
        var docs = await repository.GetByUploadedByAsync(request.TenantId, request.UserId, cancellationToken);
        return docs.Select(d => new DocumentDto(
            d.Id, d.FileName, d.ContentType, d.FileSizeBytes,
            d.UploadedById, d.CategoryId, d.EntityType, d.EntityId,
            d.Description, d.IsDeleted, d.CreatedAt)).ToList();
    }
}

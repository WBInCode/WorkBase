using WorkBase.Modules.Documents.Application.Contracts;
using WorkBase.Shared.Cqrs;
using WorkBase.Shared.Domain;
using WorkBase.Shared.Storage;

namespace WorkBase.Modules.Documents.Application.Commands;

public sealed record UploadDocumentCommand(
    string FileName, string ContentType, long FileSizeBytes,
    Stream Content, Guid UploadedById, Guid? CategoryId = null,
    string? EntityType = null, Guid? EntityId = null, string? Description = null)
    : ICommand<Guid>, ITenantRequest
{
    public Guid TenantId { get; set; }
}

public sealed class UploadDocumentHandler(
    IDocumentRepository repository, IFileStorage fileStorage) : ICommandHandler<UploadDocumentCommand, Guid>
{
    public async Task<Result<Guid>> Handle(UploadDocumentCommand request, CancellationToken cancellationToken)
    {
        var storagePath = $"documents/{request.TenantId}/{Guid.NewGuid()}/{request.FileName}";

        await fileStorage.UploadAsync("workbase", storagePath, request.Content, request.ContentType, cancellationToken);

        var document = Domain.Entities.Document.Create(
            request.TenantId, request.FileName, storagePath, request.ContentType,
            request.FileSizeBytes, request.UploadedById, request.CategoryId,
            request.EntityType, request.EntityId, request.Description);

        await repository.AddAsync(document, cancellationToken);
        await repository.SaveChangesAsync(cancellationToken);

        return document.Id;
    }
}

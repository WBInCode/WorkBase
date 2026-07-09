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
    IDocumentRepository repository, IFileStorage fileStorage, ITenantConfigService tenantConfig)
    : ICommandHandler<UploadDocumentCommand, Guid>
{
    private const string SettingsKey = "document_upload";

    public async Task<Result<Guid>> Handle(UploadDocumentCommand request, CancellationToken cancellationToken)
    {
        var settings = await tenantConfig.GetAsync<DocumentUploadSettings>(request.TenantId, SettingsKey, cancellationToken)
            ?? new DocumentUploadSettings();

        // Path.GetFileName strips any directory segments (incl. "../") an attacker could smuggle
        // in the original file name, preventing storage-path traversal.
        var safeFileName = Path.GetFileName(request.FileName);
        if (string.IsNullOrWhiteSpace(safeFileName))
            return Result.Failure<Guid>(Error.Validation("Document.InvalidFileName", "Nieprawidłowa nazwa pliku."));

        var extension = Path.GetExtension(safeFileName).ToLowerInvariant();
        if (string.IsNullOrEmpty(extension) || !settings.AllowedExtensions.Contains(extension))
            return Result.Failure<Guid>(Error.Validation("Document.ExtensionNotAllowed",
                $"Niedozwolone rozszerzenie pliku '{extension}'. Dozwolone: {string.Join(", ", settings.AllowedExtensions)}"));

        if (request.FileSizeBytes > settings.MaxFileSizeBytes)
            return Result.Failure<Guid>(Error.Validation("Document.TooLarge",
                $"Plik przekracza maksymalny dozwolony rozmiar ({settings.MaxFileSizeBytes / (1024 * 1024)} MB)."));

        var storagePath = $"documents/{request.TenantId}/{Guid.NewGuid()}/{safeFileName}";

        await fileStorage.UploadAsync("workbase", storagePath, request.Content, request.ContentType, cancellationToken);

        var document = Domain.Entities.Document.Create(
            request.TenantId, safeFileName, storagePath, request.ContentType,
            request.FileSizeBytes, request.UploadedById, request.CategoryId,
            request.EntityType, request.EntityId, request.Description);

        await repository.AddAsync(document, cancellationToken);
        await repository.SaveChangesAsync(cancellationToken);

        return document.Id;
    }
}

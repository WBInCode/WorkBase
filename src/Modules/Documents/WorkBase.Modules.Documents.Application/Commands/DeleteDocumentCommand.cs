using WorkBase.Modules.Documents.Application.Contracts;
using WorkBase.Shared.Cqrs;
using WorkBase.Shared.Domain;

namespace WorkBase.Modules.Documents.Application.Commands;

public sealed record DeleteDocumentCommand(Guid DocumentId) : ICommand, ITenantRequest
{
    public Guid TenantId { get; set; }
}

public sealed class DeleteDocumentHandler(IDocumentRepository repository) : ICommandHandler<DeleteDocumentCommand>
{
    public async Task<Result> Handle(DeleteDocumentCommand request, CancellationToken cancellationToken)
    {
        var document = await repository.GetByIdAsync(request.DocumentId, cancellationToken);
        if (document is null || document.TenantId != request.TenantId)
            return Result.Failure(Error.NotFound("Document.NotFound", "Document not found"));

        document.SoftDelete();
        repository.Update(document);
        await repository.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}

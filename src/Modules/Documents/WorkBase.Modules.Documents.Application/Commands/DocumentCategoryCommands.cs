using WorkBase.Modules.Documents.Application.Contracts;
using WorkBase.Shared.Cqrs;
using WorkBase.Shared.Domain;

namespace WorkBase.Modules.Documents.Application.Commands;

public sealed record CreateDocumentCategoryCommand(string Name, string? Description = null)
    : ICommand<Guid>, ITenantRequest
{
    public Guid TenantId { get; set; }
}

public sealed class CreateDocumentCategoryHandler(IDocumentCategoryRepository repository)
    : ICommandHandler<CreateDocumentCategoryCommand, Guid>
{
    public async Task<Result<Guid>> Handle(CreateDocumentCategoryCommand request, CancellationToken cancellationToken)
    {
        var category = Domain.Entities.DocumentCategory.Create(request.TenantId, request.Name, request.Description);
        await repository.AddAsync(category, cancellationToken);
        await repository.SaveChangesAsync(cancellationToken);
        return category.Id;
    }
}

public sealed record UpdateDocumentCategoryCommand(Guid CategoryId, string Name, string? Description = null)
    : ICommand, ITenantRequest
{
    public Guid TenantId { get; set; }
}

public sealed class UpdateDocumentCategoryHandler(IDocumentCategoryRepository repository)
    : ICommandHandler<UpdateDocumentCategoryCommand>
{
    public async Task<Result> Handle(UpdateDocumentCategoryCommand request, CancellationToken cancellationToken)
    {
        var category = await repository.GetByIdAsync(request.CategoryId, cancellationToken);
        if (category is null || category.TenantId != request.TenantId)
            return Result.Failure(Error.NotFound("Category.NotFound", "Category not found"));

        category.Update(request.Name, request.Description);
        repository.Update(category);
        await repository.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}

public sealed record DeleteDocumentCategoryCommand(Guid CategoryId) : ICommand, ITenantRequest
{
    public Guid TenantId { get; set; }
}

public sealed class DeleteDocumentCategoryHandler(IDocumentCategoryRepository repository)
    : ICommandHandler<DeleteDocumentCategoryCommand>
{
    public async Task<Result> Handle(DeleteDocumentCategoryCommand request, CancellationToken cancellationToken)
    {
        var deleted = await repository.DeleteAsync(request.CategoryId, cancellationToken);
        if (!deleted)
            return Result.Failure(Error.NotFound("Category.NotFound", "Category not found"));

        await repository.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}

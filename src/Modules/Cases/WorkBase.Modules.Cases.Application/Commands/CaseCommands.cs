using WorkBase.Modules.Cases.Application.Contracts;
using WorkBase.Modules.Cases.Domain.Entities;
using WorkBase.Shared.Cqrs;
using WorkBase.Shared.Domain;

namespace WorkBase.Modules.Cases.Application.Commands;

public sealed record CreateCaseCommand(
    string Title, string? Description, CasePriorityLevel Priority,
    Guid? CategoryId = null, Guid? AssigneeId = null, Guid? ContactId = null,
    DateTime? DueDate = null) : ICommand<Guid>, ITenantRequest
{
    public Guid TenantId { get; set; }
}

public sealed class CreateCaseHandler(
    ICaseItemRepository caseRepository,
    ICaseStatusRepository statusRepository,
    ICaseCategoryRepository categoryRepository)
    : ICommandHandler<CreateCaseCommand, Guid>
{
    public async Task<Result<Guid>> Handle(CreateCaseCommand request, CancellationToken ct)
    {
        var defaultStatus = await statusRepository.GetDefaultAsync(request.TenantId, ct);
        if (defaultStatus is null)
            return Result.Failure<Guid>(new Error("Case.NoDefaultStatus",
                "Brak domyślnego statusu sprawy."));

        if (request.CategoryId.HasValue)
        {
            var category = await categoryRepository.GetByIdAsync(request.CategoryId.Value, ct);
            if (category is null)
                return Result.Failure<Guid>(Error.NotFound("Case.CategoryNotFound",
                    $"Kategoria o id '{request.CategoryId}' nie została znaleziona."));
        }

        var nextNumber = await caseRepository.GetNextNumberAsync(request.TenantId, ct);
        var number = $"CASE-{nextNumber:D5}";

        var dueDate = request.DueDate;
        if (!dueDate.HasValue && request.CategoryId.HasValue)
        {
            var cat = await categoryRepository.GetByIdAsync(request.CategoryId.Value, ct);
            if (cat?.DefaultSla.HasValue == true)
                dueDate = DateTime.UtcNow.Add(cat.DefaultSla.Value);
        }

        var caseItem = CaseItem.Create(
            request.TenantId, number, request.Title, defaultStatus.Id,
            request.Priority, request.CategoryId, request.AssigneeId,
            request.ContactId, request.Description, dueDate);

        await caseRepository.AddAsync(caseItem, ct);
        return caseItem.Id;
    }
}

public sealed record UpdateCaseCommand(
    Guid CaseId, string Title, string? Description,
    CasePriorityLevel Priority, DateTime? DueDate, Guid? CategoryId) : ICommand, ITenantRequest
{
    public Guid TenantId { get; set; }
}

public sealed class UpdateCaseHandler(ICaseItemRepository caseRepository) : ICommandHandler<UpdateCaseCommand>
{
    public async Task<Result> Handle(UpdateCaseCommand request, CancellationToken ct)
    {
        var item = await caseRepository.GetByIdAsync(request.CaseId, ct);
        if (item is null) return Result.Failure(Error.NotFound("Case.NotFound", "Sprawa nie została znaleziona."));
        item.Update(request.Title, request.Description, request.Priority, request.DueDate, request.CategoryId);
        caseRepository.Update(item);
        return Result.Success();
    }
}

public sealed record ChangeCaseStatusCommand(Guid CaseId, Guid NewStatusId, Guid ChangedById) : ICommand, ITenantRequest
{
    public Guid TenantId { get; set; }
}

public sealed class ChangeCaseStatusHandler(ICaseItemRepository repo, ICaseStatusRepository statusRepo) : ICommandHandler<ChangeCaseStatusCommand>
{
    public async Task<Result> Handle(ChangeCaseStatusCommand request, CancellationToken ct)
    {
        var item = await repo.GetByIdAsync(request.CaseId, ct);
        if (item is null) return Result.Failure(Error.NotFound("Case.NotFound", "Sprawa nie została znaleziona."));
        var status = await statusRepo.GetByIdAsync(request.NewStatusId, ct);
        if (status is null) return Result.Failure(Error.NotFound("Case.StatusNotFound", "Status nie został znaleziony."));
        item.ChangeStatus(request.NewStatusId, request.ChangedById);
        if (status.IsFinal) item.Close(DateTime.UtcNow);
        repo.Update(item);
        return Result.Success();
    }
}

public sealed record AssignCaseCommand(Guid CaseId, Guid? AssigneeId) : ICommand, ITenantRequest
{
    public Guid TenantId { get; set; }
}

public sealed class AssignCaseHandler(ICaseItemRepository repo) : ICommandHandler<AssignCaseCommand>
{
    public async Task<Result> Handle(AssignCaseCommand request, CancellationToken ct)
    {
        var item = await repo.GetByIdAsync(request.CaseId, ct);
        if (item is null) return Result.Failure(Error.NotFound("Case.NotFound", "Sprawa nie została znaleziona."));
        item.Assign(request.AssigneeId);
        repo.Update(item);
        return Result.Success();
    }
}

public sealed record AddCaseCommentCommand(Guid CaseId, string Content, bool IsInternal, Guid AuthorId) : ICommand<Guid>, ITenantRequest
{
    public Guid TenantId { get; set; }
}

public sealed class AddCaseCommentHandler(ICaseItemRepository caseRepo, ICaseCommentRepository commentRepo) : ICommandHandler<AddCaseCommentCommand, Guid>
{
    public async Task<Result<Guid>> Handle(AddCaseCommentCommand request, CancellationToken ct)
    {
        var item = await caseRepo.GetByIdAsync(request.CaseId, ct);
        if (item is null) return Result.Failure<Guid>(Error.NotFound("Case.NotFound", "Sprawa nie została znaleziona."));
        var comment = CaseComment.Create(request.TenantId, request.CaseId, request.AuthorId, request.Content, request.IsInternal);
        await commentRepo.AddAsync(comment, ct);
        return comment.Id;
    }
}

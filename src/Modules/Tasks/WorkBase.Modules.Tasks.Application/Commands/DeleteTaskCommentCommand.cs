using WorkBase.Modules.Tasks.Application.Contracts;
using WorkBase.Shared.Cqrs;
using WorkBase.Shared.Domain;

namespace WorkBase.Modules.Tasks.Application.Commands;

public sealed record DeleteTaskCommentCommand(
    Guid TaskId, Guid CommentId) : ICommand, ITenantRequest
{
    public Guid TenantId { get; set; }
}

public sealed class DeleteTaskCommentHandler(ITaskCommentRepository repository)
    : ICommandHandler<DeleteTaskCommentCommand>
{
    public async Task<Result> Handle(DeleteTaskCommentCommand request, CancellationToken cancellationToken)
    {
        var comment = await repository.GetByIdAsync(request.CommentId, cancellationToken);
        if (comment is null || comment.TaskId != request.TaskId)
            return Result.Failure(Error.NotFound("TaskComment.NotFound",
                $"Komentarz o id '{request.CommentId}' nie został znaleziony."));

        repository.Remove(comment);
        return Result.Success();
    }
}

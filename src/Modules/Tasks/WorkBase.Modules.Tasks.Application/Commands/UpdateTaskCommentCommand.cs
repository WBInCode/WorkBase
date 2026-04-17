using WorkBase.Modules.Tasks.Application.Contracts;
using WorkBase.Shared.Cqrs;
using WorkBase.Shared.Domain;

namespace WorkBase.Modules.Tasks.Application.Commands;

public sealed record UpdateTaskCommentCommand(
    Guid TaskId, Guid CommentId, string Content) : ICommand, ITenantRequest
{
    public Guid TenantId { get; set; }
}

public sealed class UpdateTaskCommentHandler(ITaskCommentRepository repository)
    : ICommandHandler<UpdateTaskCommentCommand>
{
    public async Task<Result> Handle(UpdateTaskCommentCommand request, CancellationToken cancellationToken)
    {
        var comment = await repository.GetByIdAsync(request.CommentId, cancellationToken);
        if (comment is null || comment.TaskId != request.TaskId)
            return Result.Failure(Error.NotFound("TaskComment.NotFound",
                $"Komentarz o id '{request.CommentId}' nie został znaleziony."));

        comment.UpdateContent(request.Content);
        return Result.Success();
    }
}

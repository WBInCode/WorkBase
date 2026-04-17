using WorkBase.Modules.Tasks.Application.Contracts;
using WorkBase.Modules.Tasks.Domain.Entities;
using WorkBase.Modules.Tasks.Domain.Events;
using WorkBase.Shared.Cqrs;
using WorkBase.Shared.Domain;

namespace WorkBase.Modules.Tasks.Application.Commands;

public sealed record AddTaskCommentCommand(
    Guid TaskId, Guid AuthorId, string Content) : ICommand<Guid>, ITenantRequest
{
    public Guid TenantId { get; set; }
}

public sealed class AddTaskCommentHandler(
    ITaskItemRepository taskRepository,
    ITaskCommentRepository commentRepository)
    : ICommandHandler<AddTaskCommentCommand, Guid>
{
    public async Task<Result<Guid>> Handle(AddTaskCommentCommand request, CancellationToken cancellationToken)
    {
        var task = await taskRepository.GetByIdAsync(request.TaskId, cancellationToken);
        if (task is null)
            return Result.Failure<Guid>(Error.NotFound("Task.NotFound",
                $"Zadanie o id '{request.TaskId}' nie zostało znalezione."));

        var comment = TaskComment.Create(
            request.TenantId, request.TaskId, request.AuthorId, request.Content);

        comment.RaiseDomainEvent(new TaskCommentAddedEvent(
            comment.Id, request.TaskId, request.TenantId, request.AuthorId));

        await commentRepository.AddAsync(comment, cancellationToken);
        return comment.Id;
    }
}

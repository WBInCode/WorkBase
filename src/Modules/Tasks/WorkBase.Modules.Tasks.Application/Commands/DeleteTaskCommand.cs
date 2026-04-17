using WorkBase.Modules.Tasks.Application.Contracts;
using WorkBase.Shared.Cqrs;
using WorkBase.Shared.Domain;

namespace WorkBase.Modules.Tasks.Application.Commands;

public sealed record DeleteTaskCommand(Guid Id) : ICommand, ITenantRequest
{
    public Guid TenantId { get; set; }
}

public sealed class DeleteTaskHandler(ITaskItemRepository repository)
    : ICommandHandler<DeleteTaskCommand>
{
    public async Task<Result> Handle(DeleteTaskCommand request, CancellationToken cancellationToken)
    {
        var task = await repository.GetByIdAsync(request.Id, cancellationToken);
        if (task is null || task.TenantId != request.TenantId)
            return Result.Failure(Error.NotFound("Task.NotFound", "Zadanie nie zostało znalezione."));

        repository.Remove(task);
        return Result.Success();
    }
}

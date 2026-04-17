using WorkBase.Modules.Tasks.Application.Contracts;
using WorkBase.Shared.Cqrs;
using WorkBase.Shared.Domain;

namespace WorkBase.Modules.Tasks.Application.Commands;

public sealed record DeleteTaskPriorityCommand(Guid Id) : ICommand, ITenantRequest
{
    public Guid TenantId { get; set; }
}

public sealed class DeleteTaskPriorityHandler(ITaskPriorityRepository repository)
    : ICommandHandler<DeleteTaskPriorityCommand>
{
    public async Task<Result> Handle(DeleteTaskPriorityCommand request, CancellationToken cancellationToken)
    {
        var priority = await repository.GetByIdAsync(request.Id, cancellationToken);
        if (priority is null)
            return Result.Failure(Error.NotFound("TaskPriority.NotFound",
                $"Priorytet zadania o id '{request.Id}' nie został znaleziony."));

        repository.Remove(priority);
        return Result.Success();
    }
}

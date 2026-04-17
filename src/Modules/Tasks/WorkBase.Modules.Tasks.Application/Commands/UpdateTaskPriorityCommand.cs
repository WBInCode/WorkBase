using WorkBase.Modules.Tasks.Application.Contracts;
using WorkBase.Shared.Cqrs;
using WorkBase.Shared.Domain;

namespace WorkBase.Modules.Tasks.Application.Commands;

public sealed record UpdateTaskPriorityCommand(
    Guid Id, string Name, string? Color,
    int SortOrder) : ICommand, ITenantRequest
{
    public Guid TenantId { get; set; }
}

public sealed class UpdateTaskPriorityHandler(ITaskPriorityRepository repository)
    : ICommandHandler<UpdateTaskPriorityCommand>
{
    public async Task<Result> Handle(UpdateTaskPriorityCommand request, CancellationToken cancellationToken)
    {
        var priority = await repository.GetByIdAsync(request.Id, cancellationToken);
        if (priority is null)
            return Result.Failure(Error.NotFound("TaskPriority.NotFound",
                $"Priorytet zadania o id '{request.Id}' nie został znaleziony."));

        priority.Update(request.Name, request.Color, request.SortOrder);
        return Result.Success();
    }
}

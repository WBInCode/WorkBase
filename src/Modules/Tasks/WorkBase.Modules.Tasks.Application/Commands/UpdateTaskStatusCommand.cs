using WorkBase.Modules.Tasks.Application.Contracts;
using WorkBase.Shared.Cqrs;
using WorkBase.Shared.Domain;
using TaskStatus = WorkBase.Modules.Tasks.Domain.Entities.TaskStatus;

namespace WorkBase.Modules.Tasks.Application.Commands;

public sealed record UpdateTaskStatusCommand(
    Guid Id, string Name, string? Color,
    bool IsFinal, int SortOrder) : ICommand, ITenantRequest
{
    public Guid TenantId { get; set; }
}

public sealed class UpdateTaskStatusHandler(ITaskStatusRepository repository)
    : ICommandHandler<UpdateTaskStatusCommand>
{
    public async Task<Result> Handle(UpdateTaskStatusCommand request, CancellationToken cancellationToken)
    {
        var status = await repository.GetByIdAsync(request.Id, cancellationToken);
        if (status is null)
            return Result.Failure(Error.NotFound("TaskStatus.NotFound",
                $"Status zadania o id '{request.Id}' nie został znaleziony."));

        status.Update(request.Name, request.Color, request.IsFinal, request.SortOrder);
        return Result.Success();
    }
}

using WorkBase.Modules.Tasks.Application.Contracts;
using WorkBase.Shared.Cqrs;
using WorkBase.Shared.Domain;

namespace WorkBase.Modules.Tasks.Application.Commands;

public sealed record DeleteTaskStatusCommand(Guid Id) : ICommand, ITenantRequest
{
    public Guid TenantId { get; set; }
}

public sealed class DeleteTaskStatusHandler(ITaskStatusRepository repository)
    : ICommandHandler<DeleteTaskStatusCommand>
{
    public async Task<Result> Handle(DeleteTaskStatusCommand request, CancellationToken cancellationToken)
    {
        var status = await repository.GetByIdAsync(request.Id, cancellationToken);
        if (status is null)
            return Result.Failure(Error.NotFound("TaskStatus.NotFound",
                $"Status zadania o id '{request.Id}' nie został znaleziony."));

        repository.Remove(status);
        return Result.Success();
    }
}

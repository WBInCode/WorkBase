using WorkBase.Modules.Tasks.Application.Contracts;
using WorkBase.Shared.Cqrs;
using WorkBase.Shared.Domain;
using TaskStatus = WorkBase.Modules.Tasks.Domain.Entities.TaskStatus;

namespace WorkBase.Modules.Tasks.Application.Commands;

public sealed record CreateTaskStatusCommand(
    string Code, string Name, string? Color,
    bool IsFinal, bool IsDefault, int SortOrder) : ICommand<Guid>, ITenantRequest
{
    public Guid TenantId { get; set; }
}

public sealed class CreateTaskStatusHandler(ITaskStatusRepository repository)
    : ICommandHandler<CreateTaskStatusCommand, Guid>
{
    public async Task<Result<Guid>> Handle(CreateTaskStatusCommand request, CancellationToken cancellationToken)
    {
        var status = TaskStatus.Create(
            request.TenantId, request.Code, request.Name,
            request.IsFinal, request.IsDefault, request.Color, request.SortOrder);

        await repository.AddAsync(status, cancellationToken);
        return status.Id;
    }
}

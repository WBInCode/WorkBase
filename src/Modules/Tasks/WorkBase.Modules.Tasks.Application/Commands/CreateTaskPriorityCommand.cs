using WorkBase.Modules.Tasks.Application.Contracts;
using WorkBase.Modules.Tasks.Domain.Entities;
using WorkBase.Shared.Cqrs;
using WorkBase.Shared.Domain;

namespace WorkBase.Modules.Tasks.Application.Commands;

public sealed record CreateTaskPriorityCommand(
    string Code, string Name, string? Color,
    int SortOrder) : ICommand<Guid>, ITenantRequest
{
    public Guid TenantId { get; set; }
}

public sealed class CreateTaskPriorityHandler(ITaskPriorityRepository repository)
    : ICommandHandler<CreateTaskPriorityCommand, Guid>
{
    public async Task<Result<Guid>> Handle(CreateTaskPriorityCommand request, CancellationToken cancellationToken)
    {
        var priority = TaskPriority.Create(
            request.TenantId, request.Code, request.Name,
            request.Color, request.SortOrder);

        await repository.AddAsync(priority, cancellationToken);
        return priority.Id;
    }
}

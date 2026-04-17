using WorkBase.Modules.Organization.Application.Contracts;
using WorkBase.Shared.Cqrs;
using WorkBase.Shared.Domain;

namespace WorkBase.Modules.Organization.Application.Commands.Positions;

public sealed record DeletePositionCommand(Guid Id) : ICommand, ITenantRequest
{
    public Guid TenantId { get; set; }
}

public sealed class DeletePositionHandler(IPositionRepository repository)
    : ICommandHandler<DeletePositionCommand>
{
    public async Task<Result> Handle(DeletePositionCommand request, CancellationToken cancellationToken)
    {
        var position = await repository.GetByIdAsync(request.Id, cancellationToken);
        if (position is null || position.TenantId != request.TenantId)
            return Result.Failure(Error.NotFound("Position.NotFound", "Stanowisko nie zostało znalezione."));

        repository.Remove(position);
        return Result.Success();
    }
}

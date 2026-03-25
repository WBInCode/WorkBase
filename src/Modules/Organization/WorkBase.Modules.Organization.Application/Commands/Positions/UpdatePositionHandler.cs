using WorkBase.Modules.Organization.Application.Contracts;
using WorkBase.Shared.Cqrs;
using WorkBase.Shared.Domain;

namespace WorkBase.Modules.Organization.Application.Commands.Positions;

public sealed class UpdatePositionHandler(IPositionRepository positionRepository)
    : ICommandHandler<UpdatePositionCommand>
{
    public async Task<Result> Handle(
        UpdatePositionCommand request,
        CancellationToken cancellationToken)
    {
        var position = await positionRepository.GetByIdAsync(request.PositionId, cancellationToken);

        if (position is null)
            return Result.Failure(Error.NotFound("Position.NotFound", $"Position '{request.PositionId}' not found."));

        if (await positionRepository.NameExistsInTenantAsync(request.TenantId, request.Name, request.PositionId, cancellationToken))
            return Result.Failure(Error.Conflict("Position.NameExists", $"Position '{request.Name}' already exists."));

        position.Update(request.Name, request.Description);
        positionRepository.Update(position);

        return Result.Success();
    }
}

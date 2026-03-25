using WorkBase.Modules.Organization.Application.Contracts;
using WorkBase.Modules.Organization.Domain.Entities;
using WorkBase.Shared.Cqrs;
using WorkBase.Shared.Domain;

namespace WorkBase.Modules.Organization.Application.Commands.Positions;

public sealed class CreatePositionHandler(IPositionRepository positionRepository)
    : ICommandHandler<CreatePositionCommand, Guid>
{
    public async Task<Result<Guid>> Handle(
        CreatePositionCommand request,
        CancellationToken cancellationToken)
    {
        if (await positionRepository.NameExistsInTenantAsync(request.TenantId, request.Name, cancellationToken: cancellationToken))
            return Result.Failure<Guid>(Error.Conflict("Position.NameExists", $"Position '{request.Name}' already exists."));

        var position = Position.Create(request.TenantId, request.Name, request.Description);

        await positionRepository.AddAsync(position, cancellationToken);

        return position.Id;
    }
}

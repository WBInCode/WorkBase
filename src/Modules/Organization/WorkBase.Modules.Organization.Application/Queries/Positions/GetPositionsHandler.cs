using WorkBase.Modules.Organization.Application.Contracts;
using WorkBase.Modules.Organization.Application.Dtos;
using WorkBase.Shared.Cqrs;
using WorkBase.Shared.Domain;

namespace WorkBase.Modules.Organization.Application.Queries.Positions;

public sealed class GetPositionsHandler(IPositionRepository positionRepository)
    : IQueryHandler<GetPositionsQuery, List<PositionDto>>
{
    public async Task<Result<List<PositionDto>>> Handle(
        GetPositionsQuery request,
        CancellationToken cancellationToken)
    {
        var positions = await positionRepository.GetAllByTenantAsync(request.TenantId, cancellationToken);

        var dtos = positions
            .Select(p => new PositionDto(p.Id, p.Name, p.Description, p.IsActive))
            .ToList();

        return dtos;
    }
}

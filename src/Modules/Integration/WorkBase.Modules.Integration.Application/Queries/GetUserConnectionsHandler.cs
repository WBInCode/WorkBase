using WorkBase.Modules.Integration.Application.Contracts;
using WorkBase.Shared.Cqrs;
using WorkBase.Shared.Domain;

namespace WorkBase.Modules.Integration.Application.Queries;

internal sealed class GetUserConnectionsHandler(
    IIntegrationConnectionRepository connectionRepository) : IQueryHandler<GetUserConnectionsQuery, IReadOnlyList<ConnectionDto>>
{
    public async Task<Result<IReadOnlyList<ConnectionDto>>> Handle(GetUserConnectionsQuery request, CancellationToken cancellationToken)
    {
        var connections = await connectionRepository.GetByUserAsync(request.TenantId, request.UserId, cancellationToken);

        var dtos = connections.Select(c => new ConnectionDto(
            c.Id,
            c.Provider.ToString(),
            c.ExternalAccountId,
            c.DisplayName,
            c.IsActive,
            c.CreatedAt)).ToList();

        return Result.Success<IReadOnlyList<ConnectionDto>>(dtos);
    }
}

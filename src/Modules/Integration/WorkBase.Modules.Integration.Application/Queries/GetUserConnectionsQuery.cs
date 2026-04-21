using WorkBase.Shared.Cqrs;

namespace WorkBase.Modules.Integration.Application.Queries;

public sealed record ConnectionDto(
    Guid Id,
    string Provider,
    string ExternalAccountId,
    string DisplayName,
    bool IsActive,
    DateTime CreatedAt);

public sealed record GetUserConnectionsQuery(Guid UserId) : IQuery<IReadOnlyList<ConnectionDto>>, ITenantRequest
{
    public Guid TenantId { get; set; }
}

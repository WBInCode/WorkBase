using WorkBase.Shared.Cqrs;

namespace WorkBase.Modules.Organization.Application.Commands.Positions;

public sealed record UpdatePositionCommand(
    Guid PositionId,
    string Name,
    string? Description) : ICommand, ITenantRequest
{
    public Guid TenantId { get; set; }
}

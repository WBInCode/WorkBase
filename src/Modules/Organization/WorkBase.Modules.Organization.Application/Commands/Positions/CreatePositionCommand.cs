using WorkBase.Shared.Cqrs;

namespace WorkBase.Modules.Organization.Application.Commands.Positions;

public sealed record CreatePositionCommand(
    string Name,
    string? Description,
    Guid? DefaultRoleId = null,
    bool IsManagerial = false) : ICommand<Guid>, ITenantRequest
{
    public Guid TenantId { get; set; }
}

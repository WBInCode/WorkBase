using WorkBase.Shared.Cqrs;

namespace WorkBase.Modules.Organization.Application.Commands.Units;

public sealed record UpdateOrganizationUnitCommand(
    Guid UnitId,
    string Name,
    string? Code,
    Guid TypeId) : ICommand, ITenantRequest
{
    public Guid TenantId { get; set; }
}

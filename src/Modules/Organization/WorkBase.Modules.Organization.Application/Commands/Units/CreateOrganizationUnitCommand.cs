using WorkBase.Shared.Cqrs;

namespace WorkBase.Modules.Organization.Application.Commands.Units;

public sealed record CreateOrganizationUnitCommand(
    string Name,
    string? Code,
    Guid TypeId,
    Guid? ParentId) : ICommand<Guid>, ITenantRequest
{
    public Guid TenantId { get; set; }
}

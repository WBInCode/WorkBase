using WorkBase.Shared.Cqrs;

namespace WorkBase.Modules.Organization.Application.Commands.UnitTypes;

public sealed record CreateUnitTypeCommand(
    string Name,
    string? Description,
    int SortOrder) : ICommand<Guid>, ITenantRequest
{
    public Guid TenantId { get; set; }
}

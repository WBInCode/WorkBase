using WorkBase.Shared.Cqrs;

namespace WorkBase.Modules.Organization.Application.Commands.UnitTypes;

public sealed record UpdateUnitTypeCommand(
    Guid UnitTypeId,
    string Name,
    string? Description,
    int SortOrder) : ICommand, ITenantRequest
{
    public Guid TenantId { get; set; }
}

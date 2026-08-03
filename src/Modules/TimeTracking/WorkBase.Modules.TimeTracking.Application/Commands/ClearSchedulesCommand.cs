using WorkBase.Shared.Cqrs;
using WorkBase.Shared.Domain;

namespace WorkBase.Modules.TimeTracking.Application.Commands;

public sealed record ClearSchedulesCommand(
    IReadOnlyList<Guid> EmployeeIds,
    DateOnly From,
    DateOnly To,
    bool IncludeOrgUnitGenerated = false) : ICommand<int>, ITenantRequest
{
    public Guid TenantId { get; set; }
}

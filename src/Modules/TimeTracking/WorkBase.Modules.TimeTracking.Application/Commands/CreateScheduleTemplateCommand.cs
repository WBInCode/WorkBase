using WorkBase.Shared.Cqrs;

namespace WorkBase.Modules.TimeTracking.Application.Commands;

public sealed record CreateScheduleTemplateCommand(
    string Name,
    string Definition,
    string? Description = null) : ICommand<Guid>, ITenantRequest
{
    public Guid TenantId { get; set; }
}

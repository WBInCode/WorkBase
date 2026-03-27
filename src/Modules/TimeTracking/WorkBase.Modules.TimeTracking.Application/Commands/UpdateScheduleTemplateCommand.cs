using WorkBase.Shared.Cqrs;

namespace WorkBase.Modules.TimeTracking.Application.Commands;

public sealed record UpdateScheduleTemplateCommand(
    Guid TemplateId,
    string Name,
    string Definition,
    string? Description = null) : ICommand, ITenantRequest
{
    public Guid TenantId { get; set; }
}

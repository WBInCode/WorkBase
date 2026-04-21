using WorkBase.Modules.Integration.Domain.Enums;
using WorkBase.Shared.Cqrs;

namespace WorkBase.Modules.Integration.Application.Commands;

public sealed record DisconnectProviderCommand(
    IntegrationProvider Provider) : ICommand, ITenantRequest
{
    public Guid TenantId { get; set; }
}

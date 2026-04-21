using WorkBase.Modules.Integration.Domain.Enums;
using WorkBase.Shared.Cqrs;

namespace WorkBase.Modules.Integration.Application.Commands;

public sealed record ConnectProviderCommand(
    IntegrationProvider Provider,
    string Code,
    string RedirectUri) : ICommand<Guid>, ITenantRequest
{
    public Guid TenantId { get; set; }
}

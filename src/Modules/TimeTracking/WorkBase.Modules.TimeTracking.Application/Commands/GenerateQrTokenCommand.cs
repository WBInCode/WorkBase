using WorkBase.Modules.TimeTracking.Application.Dtos;
using WorkBase.Shared.Cqrs;

namespace WorkBase.Modules.TimeTracking.Application.Commands;

public sealed record GenerateQrTokenCommand(
    string? LocationId = null,
    int TtlSeconds = 30) : ICommand<QrTokenDto>, ITenantRequest
{
    public Guid TenantId { get; set; }
}

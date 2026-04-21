using Microsoft.AspNetCore.Http;
using WorkBase.Modules.Integration.Application.Contracts;
using WorkBase.Shared.Cqrs;
using WorkBase.Shared.Domain;

namespace WorkBase.Modules.Integration.Application.Commands;

internal sealed class DisconnectProviderHandler(
    IIntegrationConnectionRepository connectionRepository,
    IOAuthTokenRepository tokenRepository,
    IHttpContextAccessor httpContextAccessor) : ICommandHandler<DisconnectProviderCommand>
{
    public async Task<Result> Handle(DisconnectProviderCommand request, CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        if (userId is null)
            return Result.Failure(Error.Forbidden("Integration.NoUser", "Cannot determine user identity."));

        var connection = await connectionRepository.GetByUserAndProviderAsync(
            request.TenantId, userId.Value, request.Provider, cancellationToken);

        if (connection is null)
            return Result.Failure(Error.NotFound("Integration.NotConnected", "No active connection found for this provider."));

        connection.Deactivate();

        var token = await tokenRepository.GetByUserAndProviderAsync(
            request.TenantId, userId.Value, request.Provider, cancellationToken);
        token?.Revoke();

        await connectionRepository.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }

    private Guid? GetUserId()
    {
        var sub = httpContextAccessor.HttpContext?.User.FindFirst("sub")?.Value;
        return Guid.TryParse(sub, out var id) ? id : null;
    }
}

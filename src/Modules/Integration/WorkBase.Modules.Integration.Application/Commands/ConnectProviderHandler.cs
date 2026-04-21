using Microsoft.AspNetCore.Http;
using WorkBase.Modules.Integration.Application.Contracts;
using WorkBase.Modules.Integration.Application.Services;
using WorkBase.Modules.Integration.Domain.Entities;
using WorkBase.Shared.Cqrs;
using WorkBase.Shared.Domain;

namespace WorkBase.Modules.Integration.Application.Commands;

internal sealed class ConnectProviderHandler(
    IOAuthFlowService oAuthFlowService,
    ITokenEncryptionService encryptionService,
    IOAuthTokenRepository tokenRepository,
    IIntegrationConnectionRepository connectionRepository,
    IHttpContextAccessor httpContextAccessor) : ICommandHandler<ConnectProviderCommand, Guid>
{
    public async Task<Result<Guid>> Handle(ConnectProviderCommand request, CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        if (userId is null)
            return Result.Failure<Guid>(Error.Forbidden("Integration.NoUser", "Cannot determine user identity."));

        var result = await oAuthFlowService.ExchangeCodeAsync(
            request.Provider, request.Code, request.RedirectUri, cancellationToken);

        var encryptedAccess = encryptionService.Encrypt(result.AccessToken);
        var encryptedRefresh = result.RefreshToken is not null ? encryptionService.Encrypt(result.RefreshToken) : null;

        // Upsert token
        var existingToken = await tokenRepository.GetByUserAndProviderAsync(
            request.TenantId, userId.Value, request.Provider, cancellationToken);

        if (existingToken is not null)
        {
            existingToken.UpdateTokens(encryptedAccess, encryptedRefresh, result.ExpiresAtUtc);
        }
        else
        {
            var token = OAuthToken.Create(
                request.TenantId, userId.Value, request.Provider,
                encryptedAccess, encryptedRefresh, result.ExpiresAtUtc, null);
            await tokenRepository.AddAsync(token, cancellationToken);
        }

        // Upsert connection
        var existingConnection = await connectionRepository.GetByUserAndProviderAsync(
            request.TenantId, userId.Value, request.Provider, cancellationToken);

        if (existingConnection is not null)
        {
            existingConnection.Activate();
            await connectionRepository.SaveChangesAsync(cancellationToken);
            return Result.Success(existingConnection.Id);
        }

        var connection = IntegrationConnection.Create(
            request.TenantId, userId.Value, request.Provider,
            result.ExternalAccountId ?? string.Empty,
            result.DisplayName ?? request.Provider.ToString());

        await connectionRepository.AddAsync(connection, cancellationToken);
        await connectionRepository.SaveChangesAsync(cancellationToken);

        return Result.Success(connection.Id);
    }

    private Guid? GetUserId()
    {
        var sub = httpContextAccessor.HttpContext?.User.FindFirst("sub")?.Value;
        return Guid.TryParse(sub, out var id) ? id : null;
    }
}

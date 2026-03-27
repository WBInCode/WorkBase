using System.Security.Cryptography;
using WorkBase.Modules.TimeTracking.Application.Contracts;
using WorkBase.Modules.TimeTracking.Application.Dtos;
using WorkBase.Modules.TimeTracking.Domain.Entities;
using WorkBase.Shared.Cqrs;
using WorkBase.Shared.Domain;

namespace WorkBase.Modules.TimeTracking.Application.Commands;

public sealed class GenerateQrTokenHandler(IQrTokenRepository qrTokenRepository)
    : ICommandHandler<GenerateQrTokenCommand, QrTokenDto>
{
    private const int MinTtlSeconds = 10;
    private const int MaxTtlSeconds = 300;

    public async Task<Result<QrTokenDto>> Handle(
        GenerateQrTokenCommand request,
        CancellationToken cancellationToken)
    {
        var ttlSeconds = Math.Clamp(request.TtlSeconds, MinTtlSeconds, MaxTtlSeconds);
        var ttl = TimeSpan.FromSeconds(ttlSeconds);

        var tokenValue = GenerateSecureToken();

        var qrToken = QrToken.Create(
            request.TenantId,
            tokenValue,
            ttl,
            request.LocationId);

        await qrTokenRepository.AddAsync(qrToken, cancellationToken);

        return new QrTokenDto(
            qrToken.Token,
            qrToken.ExpiresAt,
            qrToken.LocationId);
    }

    private static string GenerateSecureToken()
    {
        return Convert.ToBase64String(RandomNumberGenerator.GetBytes(32))
            .Replace("+", "-")
            .Replace("/", "_")
            .TrimEnd('=');
    }
}

namespace WorkBase.Modules.TimeTracking.Application.Dtos;

public sealed record QrTokenDto(
    string Token,
    DateTime ExpiresAt,
    string? LocationId);

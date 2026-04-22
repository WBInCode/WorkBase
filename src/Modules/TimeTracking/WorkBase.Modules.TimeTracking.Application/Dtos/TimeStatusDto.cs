namespace WorkBase.Modules.TimeTracking.Application.Dtos;

public sealed record TimeStatusDto(
    string Status,
    DateTime? LastEntryTime,
    string? LastEntryType,
    TimeSpan WorkedToday,
    TimeSpan BreaksToday,
    string? CurrentBreakType);

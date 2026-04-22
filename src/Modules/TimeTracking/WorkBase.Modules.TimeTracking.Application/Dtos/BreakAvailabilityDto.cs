namespace WorkBase.Modules.TimeTracking.Application.Dtos;

public sealed record BreakAvailabilityDto(List<BreakOptionDto> Options);

public sealed record BreakOptionDto(
    string BreakType,
    string Label,
    bool Available,
    int UsedCount,
    int? MaxPerDay,
    double UsedMinutesToday,
    int? MaxMinutesPerDay,
    int? MaxMinutesPerBreak,
    string? DenialReason);

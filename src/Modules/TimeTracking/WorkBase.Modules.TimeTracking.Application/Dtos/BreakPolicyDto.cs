using WorkBase.Modules.TimeTracking.Domain.Entities;

namespace WorkBase.Modules.TimeTracking.Application.Dtos;

public sealed record BreakPolicyDto(
    Guid Id,
    string Name,
    string BreakType,
    int? MaxPerDay,
    int? MaxMinutesPerBreak,
    int? MaxMinutesPerDay,
    bool IsActive);

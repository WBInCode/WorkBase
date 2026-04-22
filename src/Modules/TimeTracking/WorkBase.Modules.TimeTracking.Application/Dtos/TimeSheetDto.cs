namespace WorkBase.Modules.TimeTracking.Application.Dtos;

public sealed record TimeSheetEntryDto(
    Guid Id,
    DateTime EntryTime,
    string Type,
    string? BreakType);

public sealed record TimeSheetDayDto(
    DateOnly Date,
    TimeSpan TotalWorked,
    TimeSpan TotalBreaks,
    TimeSpan NetWorked,
    string Status,
    string? Note,
    IReadOnlyList<TimeSheetEntryDto> Entries);

public sealed record TimeSheetPeriodDto(
    DateOnly From,
    DateOnly To,
    string Period,
    Guid EmployeeId,
    TimeSpan TotalWorked,
    TimeSpan TotalBreaks,
    TimeSpan NetWorked,
    int DaysWorked,
    int DaysIncomplete,
    IReadOnlyList<TimeSheetDayDto> Days);

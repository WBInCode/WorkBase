namespace WorkBase.Modules.TimeTracking.Application.Dtos;

public sealed record TimeSheetDayDto(
    DateOnly Date,
    TimeSpan TotalWorked,
    TimeSpan TotalBreaks,
    TimeSpan NetWorked,
    string Status,
    string? Note);

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

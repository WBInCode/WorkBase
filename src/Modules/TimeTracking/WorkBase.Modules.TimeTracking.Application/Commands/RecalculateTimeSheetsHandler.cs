using WorkBase.Modules.TimeTracking.Application.Contracts;
using WorkBase.Modules.TimeTracking.Domain.Services;
using WorkBase.Shared.Cqrs;
using WorkBase.Shared.Domain;

namespace WorkBase.Modules.TimeTracking.Application.Commands;

public sealed class RecalculateTimeSheetsHandler(
    ITimeEntryRepository timeEntryRepository,
    ITimeSheetRepository timeSheetRepository,
    ITimeSheetBulkReader bulkReader)
    : ICommandHandler<RecalculateTimeSheetsCommand, RecalculateTimeSheetsResult>
{
    private const int MaxDays = 186;

    public async Task<Result<RecalculateTimeSheetsResult>> Handle(
        RecalculateTimeSheetsCommand request,
        CancellationToken cancellationToken)
    {
        if (request.From > request.To)
            return Result.Failure<RecalculateTimeSheetsResult>(Error.Validation(
                "TimeSheet.InvalidRange",
                "Data początkowa nie może być późniejsza niż końcowa."));

        if (request.To.DayNumber - request.From.DayNumber + 1 > MaxDays)
            return Result.Failure<RecalculateTimeSheetsResult>(Error.Validation(
                "TimeSheet.RangeTooLarge",
                $"Jednorazowo można przeliczyć najwyżej {MaxDays} dni."));

        var karty = await bulkReader.GetSheetsAsync(
            request.TenantId, request.EmployeeId, request.From, request.To, cancellationToken);

        var now = DateTime.UtcNow;
        var poprawione = new List<RecalculatedDayDto>();
        var odjete = TimeSpan.Zero;

        foreach (var karta in karty)
        {
            var wpisy = await timeEntryRepository.GetEntriesAroundDateAsync(
                request.TenantId, karta.EmployeeId, karta.Date, cancellationToken);

            var wynik = WorkedTimeCalculator.ForDate(wpisy, karta.Date, now);
            if (wynik.Worked == karta.TotalWorked && wynik.Breaks == karta.TotalBreaks)
                continue;

            poprawione.Add(new RecalculatedDayDto(
                karta.EmployeeId,
                karta.Date,
                Math.Round(karta.TotalWorked.TotalHours, 2),
                Math.Round(wynik.Worked.TotalHours, 2)));

            if (karta.TotalWorked > wynik.Worked)
                odjete += karta.TotalWorked - wynik.Worked;

            karta.Recalculate(wynik.Worked, wynik.Breaks);
            timeSheetRepository.Update(karta);
        }

        return new RecalculateTimeSheetsResult(
            karty.Count,
            poprawione.Count,
            Math.Round(odjete.TotalHours, 2),
            poprawione);
    }
}

using WorkBase.Shared.Domain;

namespace WorkBase.Modules.TimeTracking.Domain.Services;

/// <summary>Zasady dla ręcznie wpisywanych i poprawianych odbić.</summary>
public static class TimeEntryRules
{
    /// <summary>Jak daleko wstecz wolno cofnąć wpis — dalej to już zamknięty okres rozliczeniowy.</summary>
    public const int MaxPastDays = 366;

    /// <summary>Zapas na rozjazd zegarów; poza tym wpisy z przyszłości nie mają sensu.</summary>
    private static readonly TimeSpan FutureTolerance = TimeSpan.FromMinutes(5);

    /// <summary>Zwraca błąd, gdy czas odbicia jest nierealny; <c>null</c>, gdy wpis jest w porządku.</summary>
    public static Error? ValidateEntryTime(DateTime entryTime, DateTime nowUtc)
    {
        var czas = entryTime.Kind == DateTimeKind.Utc ? entryTime : entryTime.ToUniversalTime();

        if (czas > nowUtc.Add(FutureTolerance))
            return Error.Validation(
                "TimeEntry.FutureTime",
                "Nie można zapisać odbicia z przyszłości.");

        if (czas < nowUtc.AddDays(-MaxPastDays))
            return Error.Validation(
                "TimeEntry.TooFarInPast",
                $"Odbicie może dotyczyć najwyżej {MaxPastDays} dni wstecz.");

        return null;
    }
}

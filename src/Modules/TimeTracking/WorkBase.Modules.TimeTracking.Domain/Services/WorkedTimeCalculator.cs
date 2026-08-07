using WorkBase.Modules.TimeTracking.Domain.Entities;

namespace WorkBase.Modules.TimeTracking.Domain.Services;

/// <summary>Czas policzony dla jednej doby wraz z informacją, czy trzeba go było przyciąć.</summary>
public readonly record struct WorkedTimeResult(
    TimeSpan Worked,
    TimeSpan Breaks,
    bool HasOpenSession,
    bool WasCapped)
{
    public TimeSpan Net => Worked - Breaks;
}

/// <summary>
/// Liczy czas pracy z par wejście-wyjście.
///
/// Wcześniej każda otwarta sesja rosła bez końca: zapomniane wyjście dawało dobę
/// dłuższą niż doba (produkcja pokazywała 97 h dziennie). Sesja liczy się więc
/// najwyżej do końca danej doby i najwyżej <see cref="MaxSessionHours"/> godzin.
///
/// Zmiana nocna należy do obu dób: wejście 22:00 i wyjście 6:00 daje 2 h pierwszego
/// dnia i 6 h drugiego. Dlatego liczymy przecięcie sesji z dobą, a nie całą sesję.
/// </summary>
public static class WorkedTimeCalculator
{
    /// <summary>Najdłuższa sesja, jaką uznajemy za prawdziwą. Dłuższa to zapomniane wyjście.</summary>
    public const int MaxSessionHours = 16;

    /// <summary>
    /// Czas przepracowany w dobie <paramref name="date"/>.
    /// <paramref name="entries"/> powinno obejmować też sąsiednie doby, żeby złapać zmiany nocne.
    /// </summary>
    public static WorkedTimeResult ForDate(
        IEnumerable<TimeEntry> entries,
        DateOnly date,
        DateTime nowUtc)
    {
        var dayStart = date.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        var dayEnd = dayStart.AddDays(1);
        var maxSession = TimeSpan.FromHours(MaxSessionHours);

        var ordered = entries.OrderBy(e => e.EntryTime).ToList();

        var worked = TimeSpan.Zero;
        var breaks = TimeSpan.Zero;
        var openSession = false;
        var capped = false;

        DateTime? clockIn = null;
        DateTime? breakStart = null;

        foreach (var entry in ordered)
        {
            switch (entry.Type)
            {
                case TimeEntryType.ClockIn:
                    // Powtórzone wejście bez wyjścia: domykamy poprzednie, żeby nie przepadło.
                    if (clockIn.HasValue)
                    {
                        var (part, wasCapped) = Overlap(clockIn.Value, entry.EntryTime, dayStart, dayEnd, maxSession);
                        worked += part;
                        capped |= wasCapped;
                    }
                    clockIn = entry.EntryTime;
                    break;

                case TimeEntryType.ClockOut:
                    if (clockIn.HasValue)
                    {
                        var (part, wasCapped) = Overlap(clockIn.Value, entry.EntryTime, dayStart, dayEnd, maxSession);
                        worked += part;
                        capped |= wasCapped;
                        clockIn = null;
                    }
                    break;

                case TimeEntryType.BreakStart:
                    breakStart = entry.EntryTime;
                    break;

                case TimeEntryType.BreakEnd:
                    if (breakStart.HasValue)
                    {
                        var (part, wasCapped) = Overlap(breakStart.Value, entry.EntryTime, dayStart, dayEnd, maxSession);
                        breaks += part;
                        capped |= wasCapped;
                        breakStart = null;
                    }
                    break;
            }
        }

        // Sesja bez wyjścia kończy się z dobą albo z chwilą obecną — nie rośnie w nieskończoność.
        if (clockIn.HasValue)
        {
            openSession = true;
            var (part, wasCapped) = Overlap(clockIn.Value, nowUtc, dayStart, dayEnd, maxSession);
            worked += part;
            capped |= wasCapped;
        }

        if (breakStart.HasValue)
        {
            var (part, wasCapped) = Overlap(breakStart.Value, nowUtc, dayStart, dayEnd, maxSession);
            breaks += part;
            capped |= wasCapped;
        }

        // Przerwy nie mogą przekroczyć czasu pracy, do którego należą.
        if (breaks > worked) breaks = worked;

        return new WorkedTimeResult(worked, breaks, openSession, capped);
    }

    /// <summary>Część odcinka [from,to) mieszcząca się w dobie, po ograniczeniu długości sesji.</summary>
    private static (TimeSpan Part, bool WasCapped) Overlap(
        DateTime from,
        DateTime to,
        DateTime dayStart,
        DateTime dayEnd,
        TimeSpan maxSession)
    {
        if (to <= from) return (TimeSpan.Zero, false);

        var capped = false;
        if (to - from > maxSession)
        {
            to = from + maxSession;
            capped = true;
        }

        var start = from > dayStart ? from : dayStart;
        var end = to < dayEnd ? to : dayEnd;
        return (end > start ? end - start : TimeSpan.Zero, capped);
    }
}

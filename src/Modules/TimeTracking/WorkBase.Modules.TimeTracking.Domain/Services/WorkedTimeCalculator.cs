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

/// <summary>Odcinek czasu po przycięciu do dopuszczalnej długości sesji.</summary>
public readonly record struct Odcinek(DateTime Od, DateTime Do, bool Przyciety)
{
    /// <summary>Część odcinka mieszcząca się w oknie [od, do).</summary>
    public TimeSpan CzescW(DateTime oknoOd, DateTime oknoDo)
    {
        var start = Od > oknoOd ? Od : oknoOd;
        var koniec = Do < oknoDo ? Do : oknoDo;
        return koniec > start ? koniec - start : TimeSpan.Zero;
    }
}

/// <summary>Sesje wyłuskane z wpisów czasu: osobno praca, osobno przerwy.</summary>
public readonly record struct SesjeCzasu(
    IReadOnlyList<Odcinek> Praca,
    IReadOnlyList<Odcinek> Przerwy,
    bool MaOtwartaSesje);

/// <summary>
/// Liczy czas pracy z par wejście-wyjście.
///
/// Wcześniej każda otwarta sesja rosła bez końca: zapomniane wyjście dawało dobę
/// dłuższą niż doba (produkcja pokazywała 97 h dziennie). Sesja liczy się więc
/// najwyżej <see cref="MaxSessionHours"/> godzin.
///
/// Zmiana nocna należy do obu dób: wejście 22:00 i wyjście 6:00 daje 2 h pierwszego
/// dnia i 6 h drugiego. Dlatego liczymy przecięcie sesji z dobą, a nie całą sesję.
///
/// Parowanie wpisów w sesje jest tu wydzielone (<see cref="Podziel"/>), bo tego samego
/// podziału potrzebuje liczenie godzin nocnych — a jest to logika na tyle subtelna, że
/// druga jej kopia rozjechałaby się przy pierwszej poprawce.
/// </summary>
public static class WorkedTimeCalculator
{
    /// <summary>Najdłuższa sesja, jaką uznajemy za prawdziwą. Dłuższa to zapomniane wyjście.</summary>
    public const int MaxSessionHours = 16;

    /// <summary>
    /// Paruje wpisy w sesje pracy i przerw, przycinając każdą do <see cref="MaxSessionHours"/>.
    /// Sesja bez wyjścia kończy się chwilą <paramref name="nowUtc"/>.
    /// </summary>
    public static SesjeCzasu Podziel(IEnumerable<TimeEntry> entries, DateTime nowUtc)
    {
        var maxSession = TimeSpan.FromHours(MaxSessionHours);
        var praca = new List<Odcinek>();
        var przerwy = new List<Odcinek>();
        var otwarta = false;

        DateTime? clockIn = null;
        DateTime? breakStart = null;

        foreach (var entry in entries.OrderBy(e => e.EntryTime))
        {
            switch (entry.Type)
            {
                case TimeEntryType.ClockIn:
                    // Powtórzone wejście bez wyjścia: domykamy poprzednie, żeby nie przepadło.
                    if (clockIn.HasValue) praca.Add(Odetnij(clockIn.Value, entry.EntryTime, maxSession));
                    clockIn = entry.EntryTime;
                    break;

                case TimeEntryType.ClockOut:
                    if (clockIn.HasValue)
                    {
                        praca.Add(Odetnij(clockIn.Value, entry.EntryTime, maxSession));
                        clockIn = null;
                    }
                    break;

                case TimeEntryType.BreakStart:
                    breakStart = entry.EntryTime;
                    break;

                case TimeEntryType.BreakEnd:
                    if (breakStart.HasValue)
                    {
                        przerwy.Add(Odetnij(breakStart.Value, entry.EntryTime, maxSession));
                        breakStart = null;
                    }
                    break;
            }
        }

        if (clockIn.HasValue)
        {
            otwarta = true;
            praca.Add(Odetnij(clockIn.Value, nowUtc, maxSession));
        }

        if (breakStart.HasValue) przerwy.Add(Odetnij(breakStart.Value, nowUtc, maxSession));

        return new SesjeCzasu(praca, przerwy, otwarta);
    }

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

        var sesje = Podziel(entries, nowUtc);

        var worked = TimeSpan.Zero;
        var breaks = TimeSpan.Zero;
        var capped = false;

        foreach (var odcinek in sesje.Praca)
        {
            worked += odcinek.CzescW(dayStart, dayEnd);
            capped |= odcinek.Przyciety;
        }

        foreach (var odcinek in sesje.Przerwy)
        {
            breaks += odcinek.CzescW(dayStart, dayEnd);
            capped |= odcinek.Przyciety;
        }

        // Przerwy nie mogą przekroczyć czasu pracy, do którego należą.
        if (breaks > worked) breaks = worked;

        return new WorkedTimeResult(worked, breaks, sesje.MaOtwartaSesje, capped);
    }

    /// <summary>
    /// Godziny przepracowane w porze nocnej w dobie <paramref name="date"/>, pomniejszone
    /// o przerwy przypadające na tę porę.
    /// </summary>
    /// <remarks>
    /// Porę nocną wyznacza firma (<paramref name="nocOd"/>, <paramref name="nocDo"/>) — system
    /// niczego nie narzuca. Okno przechodzące przez północ obsługiwane jest jako dwa odcinki:
    /// od początku doby do <paramref name="nocDo"/> i od <paramref name="nocOd"/> do jej końca.
    /// </remarks>
    public static TimeSpan GodzinyNocneWDobie(
        IEnumerable<TimeEntry> entries,
        DateOnly date,
        TimeOnly nocOd,
        TimeOnly nocDo,
        DateTime nowUtc)
    {
        var dayStart = date.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        var dayEnd = dayStart.AddDays(1);

        var okna = new List<(DateTime Od, DateTime Do)>();
        if (nocOd < nocDo)
        {
            // Okno w obrębie jednej doby, np. 00:00–06:00.
            okna.Add((dayStart.Add(nocOd.ToTimeSpan()), dayStart.Add(nocDo.ToTimeSpan())));
        }
        else
        {
            // Okno przez północ, np. 22:00–06:00.
            okna.Add((dayStart, dayStart.Add(nocDo.ToTimeSpan())));
            okna.Add((dayStart.Add(nocOd.ToTimeSpan()), dayEnd));
        }

        var sesje = Podziel(entries, nowUtc);

        var nocnaPraca = TimeSpan.Zero;
        var nocnePrzerwy = TimeSpan.Zero;

        foreach (var (od, do_) in okna)
        {
            foreach (var odcinek in sesje.Praca) nocnaPraca += odcinek.CzescW(od, do_);
            foreach (var odcinek in sesje.Przerwy) nocnePrzerwy += odcinek.CzescW(od, do_);
        }

        if (nocnePrzerwy > nocnaPraca) nocnePrzerwy = nocnaPraca;

        return nocnaPraca - nocnePrzerwy;
    }

    /// <summary>Odcinek przycięty do dopuszczalnej długości sesji.</summary>
    private static Odcinek Odetnij(DateTime od, DateTime do_, TimeSpan maxSession)
    {
        if (do_ <= od) return new Odcinek(od, od, false);

        if (do_ - od > maxSession) return new Odcinek(od, od + maxSession, true);

        return new Odcinek(od, do_, false);
    }
}

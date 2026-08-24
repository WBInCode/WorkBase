using WorkBase.Modules.TimeTracking.Domain.Entities;
using WorkBase.Modules.TimeTracking.Domain.Services;
using Xunit;

namespace WorkBase.Tests.Unit.TimeTracking;

/// <summary>
/// Godziny przepracowane w porze nocnej.
/// </summary>
/// <remarks>
/// Mnoznik nocny istnial w ustawieniach od dawna, zapisywal sie i byl pokazywany jako
/// ustawiony — ale wzor rozliczenia go nie uzywal, wiec nie wplywal na zadna kwote.
/// Zeby moc go zastosowac, trzeba najpierw wiedziec, ile godzin przypadlo na noc.
///
/// Pore nocna wyznacza firma. System niczego nie narzuca, wiec testy sprawdzaja rowniez
/// okno nietypowe (bez przechodzenia przez polnoc).
/// </remarks>
public class GodzinyNocneTests
{
    private static readonly Guid Tenant = Guid.NewGuid();
    private static readonly Guid Employee = Guid.NewGuid();

    private static readonly TimeOnly NocOd = new(22, 0);
    private static readonly TimeOnly NocDo = new(6, 0);

    private static TimeEntry Wpis(DateTime czas, TimeEntryType typ) =>
        TimeEntry.Create(Tenant, Employee, czas, typ, ClockMethod.Manual);

    private static DateTime Utc(int dzien, int godz, int min = 0) =>
        new(2026, 8, dzien, godz, min, 0, DateTimeKind.Utc);

    private static TimeSpan Nocne(IEnumerable<TimeEntry> wpisy, int dzien, DateTime teraz) =>
        WorkedTimeCalculator.GodzinyNocneWDobie(wpisy, new DateOnly(2026, 8, dzien), NocOd, NocDo, teraz);

    [Fact]
    public void Dzienna_zmiana_nie_daje_godzin_nocnych()
    {
        var wpisy = new[]
        {
            Wpis(Utc(4, 8), TimeEntryType.ClockIn),
            Wpis(Utc(4, 16), TimeEntryType.ClockOut),
        };

        Assert.Equal(TimeSpan.Zero, Nocne(wpisy, 4, Utc(4, 20)));
    }

    [Fact]
    public void Zmiana_nocna_dzieli_sie_miedzy_dwie_doby()
    {
        // Wejscie 22:00, wyjscie 6:00 nastepnego dnia — cala zmiana wypada w porze nocnej,
        // ale nalezy do dwoch dob: 2 h pierwszego dnia i 6 h drugiego.
        var wpisy = new[]
        {
            Wpis(Utc(4, 22), TimeEntryType.ClockIn),
            Wpis(Utc(5, 6), TimeEntryType.ClockOut),
        };

        Assert.Equal(TimeSpan.FromHours(2), Nocne(wpisy, 4, Utc(5, 12)));
        Assert.Equal(TimeSpan.FromHours(6), Nocne(wpisy, 5, Utc(5, 12)));
    }

    [Fact]
    public void Liczy_sie_tylko_czesc_zmiany_wpadajaca_w_noc()
    {
        // 18:00–02:00: do 22:00 to pora dzienna, potem 4 h nocnych — z czego 2 h w kazdej dobie.
        var wpisy = new[]
        {
            Wpis(Utc(4, 18), TimeEntryType.ClockIn),
            Wpis(Utc(5, 2), TimeEntryType.ClockOut),
        };

        Assert.Equal(TimeSpan.FromHours(2), Nocne(wpisy, 4, Utc(5, 12)));
        Assert.Equal(TimeSpan.FromHours(2), Nocne(wpisy, 5, Utc(5, 12)));
    }

    [Fact]
    public void Przerwa_w_nocy_nie_jest_platna_dodatkiem()
    {
        var wpisy = new[]
        {
            Wpis(Utc(4, 22), TimeEntryType.ClockIn),
            Wpis(Utc(4, 23), TimeEntryType.BreakStart),
            Wpis(Utc(4, 23, 30), TimeEntryType.BreakEnd),
            Wpis(Utc(5, 2), TimeEntryType.ClockOut),
        };

        // 22:00–24:00 to 2 h, minus pol godziny przerwy.
        Assert.Equal(TimeSpan.FromHours(1.5), Nocne(wpisy, 4, Utc(5, 12)));
    }

    [Fact]
    public void Okno_nocne_bez_przechodzenia_przez_polnoc_tez_dziala()
    {
        // Firma moze ustawic pore nocna np. na 00:00-06:00.
        var wpisy = new[]
        {
            Wpis(Utc(4, 4), TimeEntryType.ClockIn),
            Wpis(Utc(4, 10), TimeEntryType.ClockOut),
        };

        var nocne = WorkedTimeCalculator.GodzinyNocneWDobie(
            wpisy, new DateOnly(2026, 8, 4), new TimeOnly(0, 0), new TimeOnly(6, 0), Utc(4, 20));

        Assert.Equal(TimeSpan.FromHours(2), nocne);
    }

    [Fact]
    public void Zapomniane_wyjscie_nie_generuje_nocy_bez_konca()
    {
        // Sesja bez wyjscia jest przycinana do 16 h, tak samo jak przy liczeniu czasu pracy —
        // inaczej jedno zapomniane wyjscie dawaloby dodatek nocny za wiele dob.
        var wpisy = new[] { Wpis(Utc(4, 22), TimeEntryType.ClockIn) };

        var pierwszaDoba = Nocne(wpisy, 4, Utc(8, 12));
        var czwartaDoba = Nocne(wpisy, 7, Utc(8, 12));

        Assert.Equal(TimeSpan.FromHours(2), pierwszaDoba);
        Assert.Equal(TimeSpan.Zero, czwartaDoba);
    }

    [Fact]
    public void Brak_wpisow_daje_zero_a_nie_wyjatek()
    {
        Assert.Equal(TimeSpan.Zero, Nocne([], 4, Utc(4, 20)));
    }
}

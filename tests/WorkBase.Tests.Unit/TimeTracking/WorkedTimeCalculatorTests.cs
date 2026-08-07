using WorkBase.Modules.TimeTracking.Domain.Entities;
using WorkBase.Modules.TimeTracking.Domain.Services;
using Xunit;

namespace WorkBase.Tests.Unit.TimeTracking;

public class WorkedTimeCalculatorTests
{
    private static readonly Guid Tenant = Guid.NewGuid();
    private static readonly Guid Employee = Guid.NewGuid();

    private static TimeEntry Wpis(DateTime czas, TimeEntryType typ) =>
        TimeEntry.Create(Tenant, Employee, czas, typ, ClockMethod.Manual);

    private static DateTime Utc(int rok, int mies, int dzien, int godz, int min = 0) =>
        new(rok, mies, dzien, godz, min, 0, DateTimeKind.Utc);

    [Fact]
    public void Zwykla_zmiana_liczy_sie_od_wejscia_do_wyjscia()
    {
        var dzien = new DateOnly(2026, 8, 4);
        var wpisy = new[]
        {
            Wpis(Utc(2026, 8, 4, 8), TimeEntryType.ClockIn),
            Wpis(Utc(2026, 8, 4, 16), TimeEntryType.ClockOut),
        };

        var wynik = WorkedTimeCalculator.ForDate(wpisy, dzien, Utc(2026, 8, 4, 20));

        Assert.Equal(TimeSpan.FromHours(8), wynik.Worked);
        Assert.False(wynik.HasOpenSession);
        Assert.False(wynik.WasCapped);
    }

    [Fact]
    public void Przerwa_odejmuje_sie_od_czasu_netto()
    {
        var dzien = new DateOnly(2026, 8, 4);
        var wpisy = new[]
        {
            Wpis(Utc(2026, 8, 4, 8), TimeEntryType.ClockIn),
            Wpis(Utc(2026, 8, 4, 12), TimeEntryType.BreakStart),
            Wpis(Utc(2026, 8, 4, 12, 30), TimeEntryType.BreakEnd),
            Wpis(Utc(2026, 8, 4, 16), TimeEntryType.ClockOut),
        };

        var wynik = WorkedTimeCalculator.ForDate(wpisy, dzien, Utc(2026, 8, 4, 20));

        Assert.Equal(TimeSpan.FromHours(8), wynik.Worked);
        Assert.Equal(TimeSpan.FromMinutes(30), wynik.Breaks);
        Assert.Equal(TimeSpan.FromHours(7.5), wynik.Net);
    }

    [Fact]
    public void Zapomniane_wyjscie_nie_daje_wiecej_niz_doba()
    {
        // Dokladnie przypadek z produkcji: wejscie 4 sierpnia, brak wyjscia,
        // a lista pokazywala 97 godzin w jednej dobie.
        var dzien = new DateOnly(2026, 8, 4);
        var wpisy = new[] { Wpis(Utc(2026, 8, 4, 11, 17), TimeEntryType.ClockIn) };

        var wynik = WorkedTimeCalculator.ForDate(wpisy, dzien, Utc(2026, 8, 7, 13));

        // Sesja urywa sie po 16 h, wiec w tej dobie zostaje odcinek 11:17 -> polnoc.
        Assert.Equal(new TimeSpan(12, 43, 0), wynik.Worked);
        Assert.True(wynik.Worked < TimeSpan.FromHours(24));
        Assert.True(wynik.HasOpenSession);
        Assert.True(wynik.WasCapped);
    }

    [Fact]
    public void Zapomniane_wyjscie_nie_rosnie_wraz_z_uplywem_dni()
    {
        var dzien = new DateOnly(2026, 8, 4);
        var wpisy = new[] { Wpis(Utc(2026, 8, 4, 11, 17), TimeEntryType.ClockIn) };

        var poDniu = WorkedTimeCalculator.ForDate(wpisy, dzien, Utc(2026, 8, 5, 13));
        var poTygodniu = WorkedTimeCalculator.ForDate(wpisy, dzien, Utc(2026, 8, 11, 13));

        Assert.Equal(poDniu.Worked, poTygodniu.Worked);
    }

    [Fact]
    public void Trwajaca_zmiana_liczy_sie_do_teraz()
    {
        var dzien = new DateOnly(2026, 8, 4);
        var wpisy = new[] { Wpis(Utc(2026, 8, 4, 8), TimeEntryType.ClockIn) };

        var wynik = WorkedTimeCalculator.ForDate(wpisy, dzien, Utc(2026, 8, 4, 11, 30));

        Assert.Equal(TimeSpan.FromHours(3.5), wynik.Worked);
        Assert.True(wynik.HasOpenSession);
        Assert.False(wynik.WasCapped);
    }

    [Fact]
    public void Zmiana_nocna_dzieli_sie_miedzy_dwie_doby()
    {
        // 22:00 -> 6:00: dwie godziny nalezа do pierwszej doby, szesc do drugiej.
        var wpisy = new[]
        {
            Wpis(Utc(2026, 8, 4, 22), TimeEntryType.ClockIn),
            Wpis(Utc(2026, 8, 5, 6), TimeEntryType.ClockOut),
        };
        var teraz = Utc(2026, 8, 5, 12);

        var pierwszaDoba = WorkedTimeCalculator.ForDate(wpisy, new DateOnly(2026, 8, 4), teraz);
        var drugaDoba = WorkedTimeCalculator.ForDate(wpisy, new DateOnly(2026, 8, 5), teraz);

        Assert.Equal(TimeSpan.FromHours(2), pierwszaDoba.Worked);
        Assert.Equal(TimeSpan.FromHours(6), drugaDoba.Worked);
        Assert.Equal(TimeSpan.FromHours(8), pierwszaDoba.Worked + drugaDoba.Worked);
    }

    [Fact]
    public void Zmiana_nocna_w_toku_nie_przecieka_do_kolejnych_dob()
    {
        var wpisy = new[] { Wpis(Utc(2026, 8, 4, 22), TimeEntryType.ClockIn) };
        var teraz = Utc(2026, 8, 5, 3);

        var pierwszaDoba = WorkedTimeCalculator.ForDate(wpisy, new DateOnly(2026, 8, 4), teraz);
        var drugaDoba = WorkedTimeCalculator.ForDate(wpisy, new DateOnly(2026, 8, 5), teraz);

        Assert.Equal(TimeSpan.FromHours(2), pierwszaDoba.Worked);
        Assert.Equal(TimeSpan.FromHours(3), drugaDoba.Worked);
    }

    [Fact]
    public void Podwojne_wejscie_bez_wyjscia_nie_gubi_pierwszego_odcinka()
    {
        var dzien = new DateOnly(2026, 8, 4);
        var wpisy = new[]
        {
            Wpis(Utc(2026, 8, 4, 8), TimeEntryType.ClockIn),
            Wpis(Utc(2026, 8, 4, 10), TimeEntryType.ClockIn),
            Wpis(Utc(2026, 8, 4, 14), TimeEntryType.ClockOut),
        };

        var wynik = WorkedTimeCalculator.ForDate(wpisy, dzien, Utc(2026, 8, 4, 20));

        Assert.Equal(TimeSpan.FromHours(6), wynik.Worked);
    }

    [Fact]
    public void Doba_bez_wpisow_ma_zero_godzin()
    {
        var wynik = WorkedTimeCalculator.ForDate([], new DateOnly(2026, 8, 4), Utc(2026, 8, 4, 12));

        Assert.Equal(TimeSpan.Zero, wynik.Worked);
        Assert.False(wynik.HasOpenSession);
    }

    [Fact]
    public void Wyjscie_bez_wejscia_nie_liczy_ujemnego_czasu()
    {
        var dzien = new DateOnly(2026, 8, 5);
        var wpisy = new[] { Wpis(Utc(2026, 8, 5, 6), TimeEntryType.ClockOut) };

        var wynik = WorkedTimeCalculator.ForDate(wpisy, dzien, Utc(2026, 8, 5, 12));

        Assert.Equal(TimeSpan.Zero, wynik.Worked);
    }

    [Fact]
    public void Przerwa_nie_przekracza_czasu_pracy()
    {
        var dzien = new DateOnly(2026, 8, 4);
        var wpisy = new[]
        {
            Wpis(Utc(2026, 8, 4, 8), TimeEntryType.ClockIn),
            Wpis(Utc(2026, 8, 4, 9), TimeEntryType.ClockOut),
            Wpis(Utc(2026, 8, 4, 10), TimeEntryType.BreakStart),
            Wpis(Utc(2026, 8, 4, 16), TimeEntryType.BreakEnd),
        };

        var wynik = WorkedTimeCalculator.ForDate(wpisy, dzien, Utc(2026, 8, 4, 20));

        Assert.True(wynik.Breaks <= wynik.Worked);
        Assert.True(wynik.Net >= TimeSpan.Zero);
    }

    [Fact]
    public void Suma_dob_tygodnia_miesci_sie_w_realnych_granicach()
    {
        // Zapomniane wyjscie w poniedzialek nie moze zawyzyc calego tygodnia.
        var wpisy = new[] { Wpis(Utc(2026, 8, 3, 8), TimeEntryType.ClockIn) };
        var teraz = Utc(2026, 8, 9, 23);

        var suma = TimeSpan.Zero;
        for (var d = 3; d <= 9; d++)
            suma += WorkedTimeCalculator.ForDate(wpisy, new DateOnly(2026, 8, d), teraz).Worked;

        Assert.Equal(TimeSpan.FromHours(WorkedTimeCalculator.MaxSessionHours), suma);
    }
}

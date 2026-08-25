using WorkBase.Modules.TimeTracking.Domain.Services;
using Xunit;

namespace WorkBase.Tests.Unit.TimeTracking;

/// <summary>
/// Propozycja dni wolnych do wstawienia do kalendarza firmy.
/// </summary>
/// <remarks>
/// Swieta ruchome licza sie z daty Wielkanocy, a blad w tym rachunku jest cichy: daty nadal
/// wygladaja sensownie, tylko wypadaja w zlym dniu. Dlatego sprawdzamy je wobec dat znanych
/// z kalendarza, a nie wobec samego algorytmu.
/// </remarks>
public class KalendarzPolskiTests
{
    [Theory]
    [InlineData(2024, 3, 31)]
    [InlineData(2025, 4, 20)]
    [InlineData(2026, 4, 5)]
    [InlineData(2027, 3, 28)]
    [InlineData(2030, 4, 21)]
    public void Wielkanoc_zgadza_sie_z_kalendarzem(int rok, int miesiac, int dzien)
    {
        Assert.Equal(new DateOnly(rok, miesiac, dzien), KalendarzPolski.Wielkanoc(rok));
    }

    [Fact]
    public void Swieta_ruchome_licza_sie_od_wielkanocy()
    {
        var dni = KalendarzPolski.ProponowaneDniWolne(2026);
        DateOnly Data(string nazwa) => dni.Single(d => d.Nazwa == nazwa).Data;

        // Wielkanoc 2026: 5 kwietnia.
        Assert.Equal(new DateOnly(2026, 4, 6), Data("Poniedziałek Wielkanocny"));
        Assert.Equal(new DateOnly(2026, 5, 24), Data("Zesłanie Ducha Świętego"));
        Assert.Equal(new DateOnly(2026, 6, 4), Data("Boże Ciało"));
    }

    [Fact]
    public void Boze_cialo_wypada_w_czwartek()
    {
        // Wlasciwosc niezalezna od roku — jesli algorytm sie rozjedzie, to sie tu wysypie.
        foreach (var rok in new[] { 2024, 2025, 2026, 2027, 2030, 2035 })
        {
            var bozeCialo = KalendarzPolski.ProponowaneDniWolne(rok)
                .Single(d => d.Nazwa == "Boże Ciało").Data;

            Assert.Equal(DayOfWeek.Thursday, bozeCialo.DayOfWeek);
        }
    }

    [Fact]
    public void Wielkanoc_zawsze_wypada_w_niedziele()
    {
        for (var rok = 2020; rok <= 2040; rok++)
        {
            Assert.Equal(DayOfWeek.Sunday, KalendarzPolski.Wielkanoc(rok).DayOfWeek);
        }
    }

    [Fact]
    public void Wszystkie_daty_naleza_do_zadanego_roku_i_sa_unikalne()
    {
        var dni = KalendarzPolski.ProponowaneDniWolne(2026);

        Assert.All(dni, d => Assert.Equal(2026, d.Data.Year));
        Assert.Equal(dni.Count, dni.Select(d => d.Data).Distinct().Count());
    }

    [Fact]
    public void Lista_zawiera_komplet_swiat_stalych()
    {
        var daty = KalendarzPolski.ProponowaneDniWolne(2026).Select(d => d.Data).ToHashSet();

        foreach (var (miesiac, dzien) in new[] { (1, 1), (1, 6), (5, 1), (5, 3), (8, 15), (11, 1), (11, 11), (12, 25), (12, 26) })
        {
            Assert.Contains(new DateOnly(2026, miesiac, dzien), daty);
        }
    }
}

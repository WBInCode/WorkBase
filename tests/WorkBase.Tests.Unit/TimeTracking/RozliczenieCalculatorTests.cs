using WorkBase.Modules.TimeTracking.Domain.Services;
using Xunit;

namespace WorkBase.Tests.Unit.TimeTracking;

/// <summary>
/// Przeliczanie czasu pracy na kwoty.
/// </summary>
/// <remarks>
/// Wczesniej wzor uzywal wylacznie mnoznika nadgodzin. Mnozniki nocny i swiateczny mozna bylo
/// ustawic, zapisywaly sie i byly pokazywane jako ustawione — ale nie wplywaly na zadna kwote.
/// Dwie firmy na produkcji mialy je ustawione i nie mialy jak tego zauwazyc.
/// </remarks>
public class RozliczenieCalculatorTests
{
    private static StawkiRozliczenia Stawki(
        decimal stawka = 50m,
        decimal nadgodziny = 1.5m,
        decimal nocny = 1.2m,
        decimal swiateczny = 2.0m)
        => new(stawka, nadgodziny, nocny, swiateczny);

    [Fact]
    public void Praca_w_ramach_normy_daje_samo_zasadnicze()
    {
        var wynik = RozliczenieCalculator.Policz(
            new SkladnikiCzasu(NormaH: 160, PrzepracowaneH: 160, NocneH: 0, SwiateczneH: 0),
            Stawki());

        Assert.Equal(160m, wynik.ZwykleH);
        Assert.Equal(0m, wynik.NadgodzinyH);
        Assert.Equal(8000m, wynik.Zasadnicze);
        Assert.Equal(0m, wynik.ZaNadgodziny);
        Assert.Equal(8000m, wynik.Razem);
    }

    [Fact]
    public void Nadwyzka_ponad_norme_jest_nadgodzinami()
    {
        var wynik = RozliczenieCalculator.Policz(
            new SkladnikiCzasu(160, 168, 0, 0), Stawki());

        Assert.Equal(160m, wynik.ZwykleH);
        Assert.Equal(8m, wynik.NadgodzinyH);
        Assert.Equal(8000m, wynik.Zasadnicze);
        Assert.Equal(600m, wynik.ZaNadgodziny);   // 50 x 1,5 x 8
        Assert.Equal(8600m, wynik.Razem);
    }

    [Fact]
    public void Dodatek_nocny_wchodzi_do_rozliczenia()
    {
        // To jest dokladnie to, czego wczesniej brakowalo: 10 h nocnych przy mnozniku 1,2
        // daje 50 x 0,2 x 10 = 100 zl dodatku.
        var wynik = RozliczenieCalculator.Policz(
            new SkladnikiCzasu(160, 160, NocneH: 10, SwiateczneH: 0), Stawki());

        Assert.Equal(100m, wynik.DodatekNocny);
        Assert.Equal(8100m, wynik.Razem);
    }

    [Fact]
    public void Dodatek_swiateczny_wchodzi_do_rozliczenia()
    {
        var wynik = RozliczenieCalculator.Policz(
            new SkladnikiCzasu(160, 168, NocneH: 0, SwiateczneH: 8), Stawki());

        Assert.Equal(400m, wynik.DodatekSwiateczny);  // 50 x 1,0 x 8
        Assert.Equal(9000m, wynik.Razem);             // 8000 + 600 + 400
    }

    /// <summary>
    /// Kluczowa wlasnosc wybranego modelu: godzina nocna, ktora jest jednoczesnie nadgodzina,
    /// nie jest platna dwa razy. Dostaje wynagrodzenie za nadgodzine plus sam dodatek nocny.
    /// </summary>
    [Fact]
    public void Nocna_nadgodzina_nie_jest_liczona_podwojnie()
    {
        var wynik = RozliczenieCalculator.Policz(
            new SkladnikiCzasu(160, 168, NocneH: 8, SwiateczneH: 0), Stawki());

        Assert.Equal(8000m, wynik.Zasadnicze);
        Assert.Equal(600m, wynik.ZaNadgodziny);   // 8 h jako nadgodziny
        Assert.Equal(80m, wynik.DodatekNocny);    // te same 8 h dostaja tylko dodatek
        Assert.Equal(8680m, wynik.Razem);
    }

    [Fact]
    public void Mnoznik_rowny_jeden_wylacza_dodatek()
    {
        // Firma, ktora nie placi dodatku nocnego, zostawia jedynke i nic sie nie dolicza.
        var wynik = RozliczenieCalculator.Policz(
            new SkladnikiCzasu(160, 160, NocneH: 40, SwiateczneH: 0),
            Stawki(nocny: 1m));

        Assert.Equal(0m, wynik.DodatekNocny);
        Assert.Equal(8000m, wynik.Razem);
    }

    [Fact]
    public void Mnoznik_ponizej_jedynki_nie_obniza_wynagrodzenia()
    {
        var wynik = RozliczenieCalculator.Policz(
            new SkladnikiCzasu(160, 160, NocneH: 40, SwiateczneH: 0),
            Stawki(nocny: 0.5m));

        Assert.Equal(0m, wynik.DodatekNocny);
        Assert.Equal(8000m, wynik.Razem);
    }

    [Fact]
    public void Bez_grafiku_caly_czas_jest_zwykly_a_nie_nadliczbowy()
    {
        // Norma zero oznacza brak grafiku, a nie norme rowna zeru — inaczej kazda przepracowana
        // godzina bylaby nadgodzina i rozliczenie pokazywaloby absurdalne kwoty.
        var wynik = RozliczenieCalculator.Policz(
            new SkladnikiCzasu(NormaH: 0, PrzepracowaneH: 100, NocneH: 0, SwiateczneH: 0),
            Stawki());

        Assert.Equal(100m, wynik.ZwykleH);
        Assert.Equal(0m, wynik.NadgodzinyH);
        Assert.Equal(5000m, wynik.Razem);
    }

    [Fact]
    public void Niedopracowanie_normy_nie_daje_ujemnych_nadgodzin()
    {
        var wynik = RozliczenieCalculator.Policz(
            new SkladnikiCzasu(160, 120, 0, 0), Stawki());

        Assert.Equal(120m, wynik.ZwykleH);
        Assert.Equal(0m, wynik.NadgodzinyH);
        Assert.Equal(6000m, wynik.Razem);
    }

    [Fact]
    public void Brak_stawki_daje_zerowe_kwoty_ale_zachowuje_godziny()
    {
        var wynik = RozliczenieCalculator.Policz(
            new SkladnikiCzasu(160, 168, 8, 8), Stawki(stawka: 0m));

        Assert.Equal(8m, wynik.NadgodzinyH);
        Assert.Equal(0m, wynik.Razem);
    }
}

using WorkBase.Modules.Organization.Domain.Entities;
using Xunit;

namespace WorkBase.Tests.Unit;

/// <summary>
/// Zastepstwo w akceptacji wnioskow — reguly samej encji.
/// </summary>
/// <remarks>
/// Bez zastepstwa urlop kierownika zatrzymywal wnioski calego zespolu: akceptanta wyznacza
/// relacja przelozonego, a reguly eskalacji reaguja dopiero po przekroczeniu terminu.
/// </remarks>
public class ZastepstwoTests
{
    private static readonly Guid Firma = Guid.NewGuid();
    private static readonly Guid Kierownik = Guid.NewGuid();
    private static readonly Guid Zastepca = Guid.NewGuid();

    private static DateOnly D(int dzien) => new(2026, 9, dzien);

    [Fact]
    public void Nie_mozna_wyznaczyc_samego_siebie()
    {
        var wynik = Zastepstwo.Utworz(Firma, Kierownik, Kierownik, D(1), D(5));

        Assert.True(wynik.IsFailure);
        Assert.Equal("Zastepstwo.SamSiebie", wynik.Error.Code);
    }

    [Fact]
    public void Nie_mozna_zakonczyc_zastepstwa_przed_rozpoczeciem()
    {
        var wynik = Zastepstwo.Utworz(Firma, Kierownik, Zastepca, D(10), D(3));

        Assert.True(wynik.IsFailure);
        Assert.Equal("Zastepstwo.ZlyZakres", wynik.Error.Code);
    }

    [Fact]
    public void Zastepstwo_jednodniowe_jest_dozwolone()
    {
        var wynik = Zastepstwo.Utworz(Firma, Kierownik, Zastepca, D(7), D(7));

        Assert.True(wynik.IsSuccess);
        Assert.True(wynik.Value.ObowiazujeW(D(7)));
    }

    [Theory]
    [InlineData(4, false)]  // dzien przed
    [InlineData(5, true)]   // pierwszy dzien — wlacznie
    [InlineData(7, true)]   // srodek
    [InlineData(10, true)]  // ostatni dzien — wlacznie
    [InlineData(11, false)] // dzien po
    public void Granice_zakresu_sa_obustronnie_domkniete(int dzien, bool oczekiwane)
    {
        var zastepstwo = Zastepstwo.Utworz(Firma, Kierownik, Zastepca, D(5), D(10)).Value;

        Assert.Equal(oczekiwane, zastepstwo.ObowiazujeW(D(dzien)));
    }

    [Fact]
    public void Odwolane_zastepstwo_przestaje_obowiazywac()
    {
        var zastepstwo = Zastepstwo.Utworz(Firma, Kierownik, Zastepca, D(5), D(10)).Value;
        Assert.True(zastepstwo.ObowiazujeW(D(7)));

        zastepstwo.Odwolaj();

        Assert.False(zastepstwo.ObowiazujeW(D(7)));
    }

    [Theory]
    [InlineData(1, 4, false)]   // konczy sie dzien przed
    [InlineData(1, 5, true)]    // styka sie pierwszym dniem
    [InlineData(6, 8, true)]    // w srodku
    [InlineData(10, 15, true)]  // styka sie ostatnim dniem
    [InlineData(11, 15, false)] // zaczyna sie dzien po
    [InlineData(1, 20, true)]   // obejmuje caly
    public void Wykrywanie_nakladania_sie_zakresow(int od, int do_, bool oczekiwane)
    {
        var istniejace = Zastepstwo.Utworz(Firma, Kierownik, Zastepca, D(5), D(10)).Value;

        Assert.Equal(oczekiwane, istniejace.NakladaSieZ(D(od), D(do_)));
    }

    [Fact]
    public void Odwolane_zastepstwo_nie_blokuje_wyznaczenia_nowego()
    {
        var istniejace = Zastepstwo.Utworz(Firma, Kierownik, Zastepca, D(5), D(10)).Value;
        istniejace.Odwolaj();

        Assert.False(istniejace.NakladaSieZ(D(6), D(8)));
    }
}

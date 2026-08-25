using WorkBase.Infrastructure.Seeding;
using Xunit;

namespace WorkBase.Tests.Unit;

/// <summary>
/// Identyfikatory uprawnien nie moga sie przesuwac.
/// </summary>
/// <remarks>
/// IamSeeder liczy identyfikator uprawnienia z kolejnego numeru, a petla po ModuleCatalog.All
/// idzie pierwsza. Dolozenie modulu przesuwa wiec identyfikatory wszystkich uprawnien jawnych.
///
/// Na dzialajacej bazie nie konczy sie to cichym rozjazdem, tylko awaria: seeder wstawia
/// uprawnienia po KODZIE (pomija istniejace), ale identyfikator bierze z licznika — a ten
/// wskaze wiersz, ktory juz istnieje pod innym kodem. Wstawienie lamie klucz glowny i wywraca
/// zasiew przy starcie aplikacji.
///
/// Sprawdzone na produkcji: identyfikatory 46-50 zajmuje integration.* — uprawnienia po module
/// wycofanym z katalogu, ktore zostaly w bazie celowo.
///
/// Ten test ma paść, gdy ktos zmieni liczbe albo kolejnosc modulow. To nie jest falszywy alarm:
/// zanim taka zmiana bedzie bezpieczna, identyfikatory trzeba uniezaleznic od pozycji.
/// </remarks>
public class IamSeederIdentyfikatoryTests
{
    private static Guid Id(int numer) => Guid.Parse($"20000000-0000-0000-0000-{numer:D12}");

    [Theory]
    // Pierwszy modul katalogu — kotwica poczatku numeracji.
    [InlineData("org.view", 1)]
    // Ostatni modul katalogu i jego ostatnia akcja — kotwica konca petli po modulach.
    [InlineData("documents.export", 45)]
    // Pierwsze uprawnienie jawne. To ono przesuwa sie przy kazdej zmianie liczby modulow.
    [InlineData("org.import", 46)]
    // OSTATNIE uprawnienie z licznika. Bez tej kotwicy wstawienie czegokolwiek w srodek ciagu
    // przechodzilo niezauwazone — i dokladnie tak sie stalo przy dodawaniu uprawnien wnioskow.
    [InlineData("payroll.view-team", 78)]
    // Uprawnienia z zarezerwowanego zakresu. Ich numery nie zaleza od liczby modulow.
    [InlineData("wnioski.view", 201)]
    [InlineData("wnioski.manage", 203)]
    public void Uprawnienia_maja_ustalone_identyfikatory(string kod, int oczekiwanyNumer)
    {
        Assert.True(IamSeeder.AllPermissionIds.TryGetValue(kod, out var id),
            $"Brak uprawnienia '{kod}' w slowniku — zmienil sie zestaw uprawnien.");

        Assert.True(Id(oczekiwanyNumer) == id,
            $"Identyfikator '{kod}' mial miec numer {oczekiwanyNumer}, a ma {Numer(id)}. " +
            "Najczestsza przyczyna: dodano lub usunieto modul w ModuleCatalog. " +
            "Na dzialajacej bazie skonczy sie to kolizja klucza glownego przy starcie aplikacji.");
    }

    /// <summary>
    /// Instalacja produkcyjna ma identyfikatory z czasow, gdy katalog mial 15 modulow — najwyzszy
    /// zajety numer to 100. Nowe uprawnienie z numerem ponizej tej granicy trafia w wiersz, ktory
    /// juz istnieje pod innym kodem, i wywraca zasiew przy starcie aplikacji.
    /// </summary>
    [Fact]
    public void Nowe_uprawnienia_uzywaja_zarezerwowanego_zakresu()
    {
        const int granicaStarejNumeracji = 100;

        var poza = IamSeeder.AllPermissionIds
            .Where(para => Numer(para.Value) > granicaStarejNumeracji && Numer(para.Value) < 200)
            .Select(para => para.Key)
            .Order()
            .ToList();

        Assert.True(poza.Count == 0,
            "Uprawnienia z numerami 101-199 sa niebezpieczne: to zakres, w ktorym instalacja " +
            "produkcyjna ma juz wiersze po starej numeracji. Nowe uprawnienia numeruj od 200: " +
            string.Join(", ", poza));
    }

    private static int Numer(Guid id)
        => int.TryParse(id.ToString()[^12..], out var numer) ? numer : 0;

    [Fact]
    public void Kazdy_identyfikator_wystepuje_tylko_raz()
    {
        var identyfikatory = IamSeeder.AllPermissionIds.Values.ToList();

        Assert.Equal(identyfikatory.Count, identyfikatory.Distinct().Count());
    }
}

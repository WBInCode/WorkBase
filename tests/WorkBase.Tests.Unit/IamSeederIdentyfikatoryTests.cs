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
    public void Uprawnienia_maja_ustalone_identyfikatory(string kod, int oczekiwanyNumer)
    {
        Assert.True(IamSeeder.AllPermissionIds.TryGetValue(kod, out var id),
            $"Brak uprawnienia '{kod}' w slowniku — zmienil sie zestaw uprawnien.");

        Assert.True(Id(oczekiwanyNumer) == id,
            $"Identyfikator '{kod}' przesunal sie z {oczekiwanyNumer} na inny. " +
            "Najczestsza przyczyna: dodano lub usunieto modul w ModuleCatalog. " +
            "Na dzialajacej bazie skonczy sie to kolizja klucza glownego przy starcie aplikacji.");
    }

    [Fact]
    public void Kazdy_identyfikator_wystepuje_tylko_raz()
    {
        var identyfikatory = IamSeeder.AllPermissionIds.Values.ToList();

        Assert.Equal(identyfikatory.Count, identyfikatory.Distinct().Count());
    }
}

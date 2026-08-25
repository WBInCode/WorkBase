namespace WorkBase.Modules.TimeTracking.Domain.Services;

/// <summary>Propozycja dnia wolnego do wstawienia do kalendarza firmy.</summary>
public readonly record struct ProponowanyDzienWolny(DateOnly Data, string Nazwa);

/// <summary>
/// Typowe dni ustawowo wolne od pracy w Polsce — jako PROPOZYCJA do wstawienia, nie regula.
/// </summary>
/// <remarks>
/// System niczego nie narzuca: ta lista nie jest nigdzie uzywana automatycznie i nie wstawia
/// sie sama przy zakladaniu firmy. Administrator moze ja wstawic jednym kliknieciem, a potem
/// dowolnie zmienic, usunac albo rozszerzyc o dni firmowe. Firma dzialajaca poza Polska po
/// prostu z niej nie korzysta.
///
/// Niedziel tu nie ma — dzien tygodnia wynika z grafiku, a nie z kalendarza swiat.
/// </remarks>
public static class KalendarzPolski
{
    public static IReadOnlyList<ProponowanyDzienWolny> ProponowaneDniWolne(int rok)
    {
        var wielkanoc = Wielkanoc(rok);

        return
        [
            new(new DateOnly(rok, 1, 1), "Nowy Rok"),
            new(new DateOnly(rok, 1, 6), "Święto Trzech Króli"),
            new(wielkanoc, "Wielkanoc"),
            new(wielkanoc.AddDays(1), "Poniedziałek Wielkanocny"),
            new(new DateOnly(rok, 5, 1), "Święto Pracy"),
            new(new DateOnly(rok, 5, 3), "Święto Konstytucji 3 Maja"),
            new(wielkanoc.AddDays(49), "Zesłanie Ducha Świętego"),
            new(wielkanoc.AddDays(60), "Boże Ciało"),
            new(new DateOnly(rok, 8, 15), "Wniebowzięcie NMP"),
            new(new DateOnly(rok, 11, 1), "Wszystkich Świętych"),
            new(new DateOnly(rok, 11, 11), "Narodowe Święto Niepodległości"),
            new(new DateOnly(rok, 12, 25), "Boże Narodzenie (pierwszy dzień)"),
            new(new DateOnly(rok, 12, 26), "Boże Narodzenie (drugi dzień)"),
        ];
    }

    /// <summary>
    /// Niedziela wielkanocna w kalendarzu gregorianskim (algorytm Meeusa/Jonesa/Butchera).
    /// Od niej odliczane sa pozostale swieta ruchome.
    /// </summary>
    public static DateOnly Wielkanoc(int rok)
    {
        var a = rok % 19;
        var b = rok / 100;
        var c = rok % 100;
        var d = b / 4;
        var e = b % 4;
        var f = (b + 8) / 25;
        var g = (b - f + 1) / 3;
        var h = ((19 * a) + b - d - g + 15) % 30;
        var i = c / 4;
        var k = c % 4;
        var l = (32 + (2 * e) + (2 * i) - h - k) % 7;
        var m = (a + (11 * h) + (22 * l)) / 451;
        var miesiac = (h + l - (7 * m) + 114) / 31;
        var dzien = ((h + l - (7 * m) + 114) % 31) + 1;

        return new DateOnly(rok, miesiac, dzien);
    }
}

namespace WorkBase.Modules.TimeTracking.Domain.Services;

/// <summary>Godziny zebrane za okres — wejście do wyliczenia kwot.</summary>
public readonly record struct SkladnikiCzasu(
    decimal NormaH,
    decimal PrzepracowaneH,
    decimal NocneH,
    decimal SwiateczneH);

/// <summary>Stawka i mnożniki firmy. Każdy mnożnik równy 1 wyłącza swój dodatek.</summary>
public readonly record struct StawkiRozliczenia(
    decimal StawkaGodzinowa,
    decimal MnoznikNadgodzin,
    decimal MnoznikNocny,
    decimal MnoznikSwiateczny);

/// <summary>Rozbicie wynagrodzenia za okres.</summary>
public readonly record struct KwotyRozliczenia(
    decimal ZwykleH,
    decimal NadgodzinyH,
    decimal Zasadnicze,
    decimal ZaNadgodziny,
    decimal DodatekNocny,
    decimal DodatekSwiateczny,
    decimal Razem);

/// <summary>
/// Przelicza czas pracy na kwoty.
/// </summary>
/// <remarks>
/// <para>
/// Dodatki nocny i świąteczny liczone są jako NADWYŻKA ponad stawkę podstawową
/// (<c>stawka × (mnożnik − 1) × godziny</c>), a nie jako osobna stawka za te godziny.
/// To decyzja projektowa i warto ją znać, bo zmienia wynik:
/// </para>
/// <list type="bullet">
///   <item>godzina nocna, która jest jednocześnie nadgodziną, nie jest liczona dwa razy —
///   dostaje wynagrodzenie za nadgodzinę plus sam dodatek nocny;</item>
///   <item>mnożnik ustawiony na 1 daje dodatek zerowy, czyli neutralnie — firma, która
///   nie płaci dodatku, po prostu zostawia jedynkę;</item>
///   <item>słowo „dodatek” znaczy to samo, co w rozmowie z kadrami, więc nikt nie musi
///   zgadywać, co system policzył.</item>
/// </list>
/// <para>
/// System nie zna żadnych progów ustawowych i niczego nie wymusza — wszystkie mnożniki
/// i pora nocna pochodzą z ustawień firmy.
/// </para>
/// </remarks>
public static class RozliczenieCalculator
{
    public static KwotyRozliczenia Policz(SkladnikiCzasu czas, StawkiRozliczenia stawki)
    {
        // Bez normy z grafiku nie da się oddzielić nadgodzin — cały czas traktujemy jako zwykły.
        var zwykleH = czas.NormaH > 0
            ? Math.Min(czas.PrzepracowaneH, czas.NormaH)
            : czas.PrzepracowaneH;

        var nadgodzinyH = czas.NormaH > 0
            ? Math.Max(czas.PrzepracowaneH - czas.NormaH, 0m)
            : 0m;

        var stawka = stawki.StawkaGodzinowa;

        var zasadnicze = stawka * zwykleH;
        var zaNadgodziny = stawka * stawki.MnoznikNadgodzin * nadgodzinyH;

        // Dodatek liczy się od godzin nocnych i świątecznych niezależnie od tego, czy były
        // zwykłe czy nadliczbowe — stąd nadwyżka ponad stawkę, a nie osobna stawka.
        var dodatekNocny = stawka * Nadwyzka(stawki.MnoznikNocny) * czas.NocneH;
        var dodatekSwiateczny = stawka * Nadwyzka(stawki.MnoznikSwiateczny) * czas.SwiateczneH;

        return new KwotyRozliczenia(
            ZwykleH: zwykleH,
            NadgodzinyH: nadgodzinyH,
            Zasadnicze: zasadnicze,
            ZaNadgodziny: zaNadgodziny,
            DodatekNocny: dodatekNocny,
            DodatekSwiateczny: dodatekSwiateczny,
            Razem: zasadnicze + zaNadgodziny + dodatekNocny + dodatekSwiateczny);
    }

    /// <summary>
    /// Część mnożnika ponad stawkę podstawową. Mnożnik poniżej 1 traktujemy jak brak dodatku —
    /// dodatek nie może obniżać wynagrodzenia.
    /// </summary>
    private static decimal Nadwyzka(decimal mnoznik) => mnoznik > 1m ? mnoznik - 1m : 0m;
}

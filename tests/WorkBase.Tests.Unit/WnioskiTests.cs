using WorkBase.Modules.Workflow.Domain.Entities;
using Xunit;

namespace WorkBase.Tests.Unit;

/// <summary>
/// Typy wnioskow i wnioski skladane na ich formularzach.
/// </summary>
/// <remarks>
/// Definicja pol zyje w JSON-ie, wiec baza nie wymusi tu niczego — cala kontrola jest jawna
/// i dlatego musi miec testy. Blad w walidacji nie objawi sie wyjatkiem, tylko wnioskiem
/// z pustym albo bezsensownym polem, ktory przejdzie caly obieg akceptacji.
/// </remarks>
public class WnioskiTests
{
    private static readonly Guid Firma = Guid.NewGuid();
    private static readonly Guid Pracownik = Guid.NewGuid();

    private static PoleWniosku Pole(
        string kod, TypPola typ = TypPola.Tekst, bool wymagane = false, string[]? opcje = null)
        => new(kod, $"Etykieta {kod}", typ, wymagane, opcje);

    private static TypWniosku Typ(params PoleWniosku[] pola)
        => TypWniosku.Utworz(Firma, "ZALICZKA", "Wniosek o zaliczkę", pola).Value;

    // ─── definicja typu ───

    [Fact]
    public void Formularz_bez_pol_nie_ma_sensu()
    {
        var wynik = TypWniosku.Utworz(Firma, "PUSTY", "Pusty", []);

        Assert.True(wynik.IsFailure);
        Assert.Equal("TypWniosku.BrakPol", wynik.Error.Code);
    }

    [Fact]
    public void Powtorzony_kod_pola_jest_odrzucany()
    {
        var wynik = TypWniosku.Utworz(Firma, "X", "X", [Pole("kwota"), Pole("kwota")]);

        Assert.True(wynik.IsFailure);
        Assert.Equal("TypWniosku.PowtorzonyKod", wynik.Error.Code);
    }

    [Fact]
    public void Lista_wyboru_bez_opcji_jest_odrzucana()
    {
        // Bez tego blad wyszedlby dopiero u pracownika, ktory nie ma czego wybrac.
        var wynik = TypWniosku.Utworz(Firma, "X", "X", [Pole("powod", TypPola.Wybor)]);

        Assert.True(wynik.IsFailure);
        Assert.Equal("TypWniosku.BrakOpcji", wynik.Error.Code);
    }

    [Fact]
    public void Kod_typu_zapisuje_sie_wielkimi_literami()
    {
        var typ = TypWniosku.Utworz(Firma, " zaliczka ", "Zaliczka", [Pole("kwota")]).Value;

        Assert.Equal("ZALICZKA", typ.Kod);
    }

    [Fact]
    public void Pola_przezywaja_zapis_i_odczyt_z_json()
    {
        var typ = Typ(Pole("kwota", TypPola.Liczba, wymagane: true), Pole("cel"));

        var pola = typ.Pola();

        Assert.Equal(2, pola.Count);
        Assert.Equal(TypPola.Liczba, pola[0].Typ);
        Assert.True(pola[0].Wymagane);
    }

    // ─── walidacja wartosci ───

    [Fact]
    public void Brak_wymaganego_pola_jest_zglaszany()
    {
        var typ = Typ(Pole("kwota", TypPola.Liczba, wymagane: true));

        var bledy = typ.SprawdzWartosci(new Dictionary<string, string?>());

        Assert.Single(bledy);
        Assert.Contains("wymagane", bledy[0]);
    }

    [Fact]
    public void Puste_pole_nieobowiazkowe_przechodzi()
    {
        var typ = Typ(Pole("uwagi"));

        Assert.Empty(typ.SprawdzWartosci(new Dictionary<string, string?> { ["uwagi"] = "  " }));
    }

    [Theory]
    [InlineData("123", true)]
    [InlineData("123.45", true)]
    [InlineData("dużo", false)]
    public void Liczba_musi_byc_liczba(string wartosc, bool poprawna)
    {
        var typ = Typ(Pole("kwota", TypPola.Liczba));

        var bledy = typ.SprawdzWartosci(new Dictionary<string, string?> { ["kwota"] = wartosc });

        Assert.Equal(poprawna, bledy.Count == 0);
    }

    [Fact]
    public void Wartosc_spoza_listy_wyboru_jest_odrzucana()
    {
        var typ = Typ(Pole("srodek", TypPola.Wybor, opcje: ["auto", "pociąg"]));

        var bledy = typ.SprawdzWartosci(new Dictionary<string, string?> { ["srodek"] = "rower" });

        Assert.Single(bledy);
    }

    [Fact]
    public void Wszystkie_bledy_wracaja_naraz()
    {
        // Pracownik ma poprawic formularz za jednym razem, a nie w pieciu podejsciach.
        var typ = Typ(
            Pole("kwota", TypPola.Liczba, wymagane: true),
            Pole("data", TypPola.Data, wymagane: true),
            Pole("srodek", TypPola.Wybor, opcje: ["auto"]));

        var bledy = typ.SprawdzWartosci(new Dictionary<string, string?>
        {
            ["kwota"] = "dużo",
            ["srodek"] = "rower",
        });

        Assert.Equal(3, bledy.Count);
    }

    // ─── zycie wniosku ───

    [Fact]
    public void Wniosek_wymagajacy_akceptacji_czeka_na_decyzje()
    {
        var wniosek = Wniosek.Zloz(Firma, Guid.NewGuid(), Pracownik,
            new Dictionary<string, string?> { ["kwota"] = "500" }, wymagaAkceptacji: true);

        Assert.Equal(StatusWniosku.Oczekuje, wniosek.Status);
        Assert.Null(wniosek.RozstrzygnietyO);
    }

    [Fact]
    public void Wniosek_bez_akceptacji_jest_zalatwiony_od_razu()
    {
        // Inaczej wisialby w "Oczekuje" na zawsze, bo nikt nie ma go rozstrzygnac.
        var wniosek = Wniosek.Zloz(Firma, Guid.NewGuid(), Pracownik,
            new Dictionary<string, string?>(), wymagaAkceptacji: false);

        Assert.Equal(StatusWniosku.Zaakceptowany, wniosek.Status);
        Assert.NotNull(wniosek.RozstrzygnietyO);
    }

    [Fact]
    public void Rozstrzygnietego_wniosku_nie_da_sie_rozstrzygnac_drugi_raz()
    {
        var wniosek = Wniosek.Zloz(Firma, Guid.NewGuid(), Pracownik,
            new Dictionary<string, string?>(), wymagaAkceptacji: true);

        Assert.True(wniosek.Zaakceptuj().IsSuccess);

        var drugaProba = wniosek.Odrzuc();
        Assert.True(drugaProba.IsFailure);
        Assert.Equal("Wniosek.JuzRozstrzygniety", drugaProba.Error.Code);
        Assert.Equal(StatusWniosku.Zaakceptowany, wniosek.Status);
    }

    [Fact]
    public void Anulowanie_dziala_tylko_przed_decyzja()
    {
        var wniosek = Wniosek.Zloz(Firma, Guid.NewGuid(), Pracownik,
            new Dictionary<string, string?>(), wymagaAkceptacji: true);

        Assert.True(wniosek.Anuluj().IsSuccess);
        Assert.True(wniosek.Zaakceptuj().IsFailure);
    }

    [Fact]
    public void Wartosci_przezywaja_zapis_i_odczyt_z_json()
    {
        var wniosek = Wniosek.Zloz(Firma, Guid.NewGuid(), Pracownik,
            new Dictionary<string, string?> { ["kwota"] = "500", ["cel"] = "delegacja" },
            wymagaAkceptacji: true);

        var wartosci = wniosek.Wartosci();

        Assert.Equal("500", wartosci["kwota"]);
        Assert.Equal("delegacja", wartosci["cel"]);
    }
}

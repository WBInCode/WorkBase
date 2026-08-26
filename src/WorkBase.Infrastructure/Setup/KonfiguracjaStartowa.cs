using Microsoft.AspNetCore.Http;

namespace WorkBase.Infrastructure.Setup;

/// <summary>
/// Konfigurator pierwszego startu — wspolne stale i decyzja, ktore trasy dzialaja mimo
/// nieukonczonej konfiguracji. Projekt calosci: docs/KONFIGURATOR-PIERWSZEGO-STARTU.md.
/// </summary>
public static class KonfiguracjaStartowa
{
    /// <summary>
    /// Znacznik zapisywany WYLACZNIE przy tworzeniu nowej firmy.
    /// </summary>
    /// <remarks>
    /// To jest zabezpieczenie z definicji, a nie z ostroznosci: firmy zalozone przed
    /// powstaniem kreatora nigdy nie dostana tego klucza, wiec blokada nie moze ich dotknac.
    /// Gdyby zamiast tego pytac „czy firma wyglada na skonfigurowana", pierwsza pomylka
    /// w heurystyce zamykalaby dostep dzialajacej firmie.
    /// </remarks>
    public const string KluczWymagana = "setup.required";

    /// <summary>Znacznik czasu ukonczenia kreatora. Jego brak przy KluczWymagana = blokada.</summary>
    public const string KluczUkonczona = "setup.completed_at";

    /// <summary>Ostatni ukonczony krok kreatora — dzieki temu kreator jest wznawialny.</summary>
    public const string KluczAktualnyKrok = "setup.current_step";

    /// <summary>Kroki swiadomie pominiete, rozdzielone przecinkiem.</summary>
    public const string KluczPominieteKroki = "setup.skipped_steps";

    /// <summary>
    /// Trzy pytania kreatora, w kolejnosci. Ekran powitalny i podsumowanie nie sa krokami,
    /// bo nie zapadaja na nich zadne decyzje — nie ma czego wznawiac.
    /// </summary>
    public static class Kroki
    {
        public const string Ludzie = "ludzie";
        public const string Godziny = "godziny";
        public const string Akceptanci = "akceptanci";

        public static readonly string[] WKolejnosci = [Ludzie, Godziny, Akceptanci];

        public static bool Znany(string krok) =>
            WKolejnosci.Contains(krok, StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>Kod bledu, po ktorym interfejs przenosi uzytkownika do kreatora.</summary>
    public const string KodBledu = "SETUP_REQUIRED";

    /// <summary>
    /// Trasy dzialajace mimo nieukonczonej konfiguracji.
    /// </summary>
    /// <remarks>
    /// Kazdy wpis ma powod, bo to jest lista, na ktorej najlatwiej zamknac sobie aplikacje:
    ///  - /api/setup      — sam kreator, inaczej nie da sie go ukonczyc,
    ///  - /api/auth       — /me, z ktorego interfejs czyta uprawnienia i firme,
    ///  - /api/hub, /sso  — logowanie z Huba i webhooki; ich blokada odcina wejscie do systemu,
    ///  - /api/onboarding — rejestracja samoobslugowa, dziala bez zalogowania,
    ///  - /api/billing    — webhook Stripe'a, przychodzi z zewnatrz bez kontekstu firmy,
    ///  - /hubs           — SignalR,
    ///  - /health, /openapi, /hangfire, / — techniczne, poza kontekstem firmy.
    /// </remarks>
    private static readonly string[] DostepneBezKonfiguracji =
    [
        "/api/setup",
        "/api/auth",
        "/api/hub",
        "/sso",
        "/api/onboarding",
        "/api/billing",
        "/hubs",
        "/health",
        "/openapi",
        "/hangfire",
        "/scalar",
    ];

    public static bool SciezkaDostepnaBezKonfiguracji(PathString sciezka)
    {
        if (!sciezka.HasValue) return true;

        var wartosc = sciezka.Value!;
        if (wartosc == "/") return true;

        foreach (var prefiks in DostepneBezKonfiguracji)
        {
            if (!wartosc.StartsWith(prefiks, StringComparison.OrdinalIgnoreCase)) continue;

            // Granica segmentu jest istotna: samo StartsWith przepuscilo by "/api/setupowanie"
            // czy "/api/authors", bo zaczynaja sie tak samo jak wpis z listy. Dzis takich tras
            // nie ma, ale to jest lista, na ktorej pomylka otwiera aplikacje, nie zamyka.
            var dalej = wartosc.Length > prefiks.Length ? wartosc[prefiks.Length] : '/';
            if (dalej is '/' or '?') return true;
        }

        return false;
    }
}

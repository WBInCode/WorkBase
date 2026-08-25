using System.Text.Json;
using System.Text.Json.Serialization;
using WorkBase.Shared.Domain;

namespace WorkBase.Modules.Workflow.Domain.Entities;

/// <summary>Rodzaj pola na formularzu wniosku.</summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum TypPola
{
    Tekst = 0,
    Wielolinijkowy = 1,
    Liczba = 2,
    Data = 3,
    Wybor = 4,
    TakNie = 5,
}

/// <summary>Jedno pole formularza wniosku, definiowane przez administratora.</summary>
public sealed record PoleWniosku(
    string Kod,
    string Etykieta,
    TypPola Typ,
    bool Wymagane = false,
    IReadOnlyList<string>? Opcje = null,
    string? Podpowiedz = null);

/// <summary>
/// Typ wniosku definiowany przez firmę — zaliczka, delegacja, praca zdalna, wniosek o sprzęt.
/// </summary>
/// <remarks>
/// Silnik obiegów jest całkowicie ogólny: instancja trzyma <c>EntityType</c> jako dowolny tekst,
/// a kroki, decyzje, eskalacje i historia działają niezależnie od tego, czego dotyczą. Mimo to
/// używano go do dwóch rzeczy — wniosku urlopowego i akceptacji zadania. Typy wniosków pozwalają
/// firmie postawić na tym samym silniku własne procesy, bez pisania kodu.
///
/// Definicja pól trzymana jest jako JSON, bo jej kształt ustala firma i zmienia się bez migracji.
/// Cena: walidacja musi być jawna — patrz <see cref="SprawdzWartosci"/>.
/// </remarks>
public sealed class TypWniosku : AuditableEntity<Guid>, ITenantScoped, IAuditable
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    public Guid TenantId { get; private set; }

    /// <summary>Stały identyfikator tekstowy, np. ZALICZKA. Nie zmienia się po utworzeniu.</summary>
    public string Kod { get; private set; } = null!;

    public string Nazwa { get; private set; } = null!;

    public string? Opis { get; private set; }

    /// <summary>Definicja pól formularza w JSON.</summary>
    public string PolaJson { get; private set; } = "[]";

    /// <summary>Czy wniosek trafia do akceptacji przełożonego, czy tylko do wiadomości.</summary>
    public bool WymagaAkceptacji { get; private set; }

    public bool Aktywny { get; private set; } = true;

    private TypWniosku() { }

    public IReadOnlyList<PoleWniosku> Pola()
        => JsonSerializer.Deserialize<List<PoleWniosku>>(PolaJson, Json) ?? [];

    public static Result<TypWniosku> Utworz(
        Guid tenantId,
        string kod,
        string nazwa,
        IReadOnlyList<PoleWniosku> pola,
        bool wymagaAkceptacji = true,
        string? opis = null)
    {
        if (string.IsNullOrWhiteSpace(kod))
            return Result.Failure<TypWniosku>(new Error("TypWniosku.BrakKodu", "Typ wniosku musi mieć kod."));

        if (string.IsNullOrWhiteSpace(nazwa))
            return Result.Failure<TypWniosku>(new Error("TypWniosku.BrakNazwy", "Typ wniosku musi mieć nazwę."));

        var bladPol = SprawdzDefinicjePol(pola);
        if (bladPol is not null) return Result.Failure<TypWniosku>(bladPol);

        return new TypWniosku
        {
            Id = Guid.CreateVersion7(),
            TenantId = tenantId,
            Kod = kod.Trim().ToUpperInvariant(),
            Nazwa = nazwa.Trim(),
            Opis = string.IsNullOrWhiteSpace(opis) ? null : opis.Trim(),
            PolaJson = JsonSerializer.Serialize(pola, Json),
            WymagaAkceptacji = wymagaAkceptacji,
        };
    }

    public Result Zmien(
        string nazwa,
        IReadOnlyList<PoleWniosku> pola,
        bool wymagaAkceptacji,
        string? opis)
    {
        if (string.IsNullOrWhiteSpace(nazwa))
            return Result.Failure(new Error("TypWniosku.BrakNazwy", "Typ wniosku musi mieć nazwę."));

        var bladPol = SprawdzDefinicjePol(pola);
        if (bladPol is not null) return Result.Failure(bladPol);

        Nazwa = nazwa.Trim();
        Opis = string.IsNullOrWhiteSpace(opis) ? null : opis.Trim();
        PolaJson = JsonSerializer.Serialize(pola, Json);
        WymagaAkceptacji = wymagaAkceptacji;

        return Result.Success();
    }

    public void Wlacz() => Aktywny = true;

    public void Wylacz() => Aktywny = false;

    private static Error? SprawdzDefinicjePol(IReadOnlyList<PoleWniosku> pola)
    {
        if (pola.Count == 0)
            return new Error("TypWniosku.BrakPol", "Formularz musi mieć przynajmniej jedno pole.");

        var kody = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var pole in pola)
        {
            if (string.IsNullOrWhiteSpace(pole.Kod) || string.IsNullOrWhiteSpace(pole.Etykieta))
                return new Error("TypWniosku.PoleBezNazwy", "Każde pole musi mieć kod i etykietę.");

            if (!kody.Add(pole.Kod))
                return new Error("TypWniosku.PowtorzonyKod", $"Pole o kodzie „{pole.Kod}” występuje dwa razy.");

            // Lista wyboru bez opcji nie da sie wypelnic, a blad wyszedlby dopiero u pracownika.
            if (pole.Typ == TypPola.Wybor && (pole.Opcje is null || pole.Opcje.Count == 0))
                return new Error("TypWniosku.BrakOpcji", $"Pole „{pole.Etykieta}” jest listą wyboru, ale nie ma opcji.");
        }

        return null;
    }

    /// <summary>
    /// Sprawdza wartości wpisane przez pracownika wobec definicji pól.
    /// </summary>
    /// <remarks>
    /// Definicja pól żyje w JSON-ie, więc baza nie wymusi tu niczego — cała kontrola musi być
    /// jawna. Zwraca listę komunikatów, a nie pierwszy błąd, żeby pracownik poprawił formularz
    /// za jednym razem, a nie w pięciu podejściach.
    /// </remarks>
    public IReadOnlyList<string> SprawdzWartosci(IReadOnlyDictionary<string, string?> wartosci)
    {
        var bledy = new List<string>();

        foreach (var pole in Pola())
        {
            wartosci.TryGetValue(pole.Kod, out var wartosc);
            var pusta = string.IsNullOrWhiteSpace(wartosc);

            if (pusta)
            {
                if (pole.Wymagane) bledy.Add($"Pole „{pole.Etykieta}” jest wymagane.");
                continue;
            }

            switch (pole.Typ)
            {
                case TypPola.Liczba when !decimal.TryParse(
                    wartosc, System.Globalization.NumberStyles.Any,
                    System.Globalization.CultureInfo.InvariantCulture, out _):
                    bledy.Add($"Pole „{pole.Etykieta}” musi być liczbą.");
                    break;

                case TypPola.Data when !DateOnly.TryParse(
                    wartosc, System.Globalization.CultureInfo.InvariantCulture, out _):
                    bledy.Add($"Pole „{pole.Etykieta}” musi być datą.");
                    break;

                case TypPola.Wybor when pole.Opcje is not null
                                        && !pole.Opcje.Contains(wartosc!, StringComparer.Ordinal):
                    bledy.Add($"Pole „{pole.Etykieta}” ma wartość spoza listy.");
                    break;

                case TypPola.TakNie when !bool.TryParse(wartosc, out _):
                    bledy.Add($"Pole „{pole.Etykieta}” musi być zaznaczone albo nie.");
                    break;
            }
        }

        return bledy;
    }
}

using WorkBase.Shared.Domain;

namespace WorkBase.Modules.Organization.Domain.Entities;

/// <summary>
/// Rzecz firmy wydana pracownikowi: laptop, telefon, klucze, odzież robocza, narzędzia.
/// </summary>
/// <remarks>
/// <para>
/// Odpowiada na pytanie, które zadaje sobie każda firma powyżej dwudziestu osób i praktycznie
/// każda odpowiada arkuszem: „co ten człowiek ma od nas i co ma oddać, gdy odejdzie".
/// </para>
/// <para>
/// <b>Zwrot nie kasuje wpisu.</b> Rzecz zwrócona zostaje z datą zwrotu — historia „kto miał
/// ten laptop przede mną" jest tak samo potrzebna jak stan bieżący, a przy sporze o uszkodzenie
/// to jedyny dowód, kiedy sprzęt zmienił ręce.
/// </para>
/// <para>
/// <b>Potwierdzenie odbioru składa wyłącznie pracownik</b>, we własnym imieniu, z własnego konta.
/// Kadry mogą wpisać wydanie, ale nie mogą „potwierdzić za niego" — wtedy potwierdzenie nic by
/// nie znaczyło. Brak potwierdzenia niczego nie blokuje; to informacja, nie bramka.
/// </para>
/// <para>
/// Rodzaj jest tekstem, nie słownikiem. Firmy wydają bardzo różne rzeczy i lista z góry byłaby
/// albo za krótka, albo za długa; interfejs podpowiada typowe wartości, ale nie zmusza do nich.
/// ponytail: słownik rodzajów jak przy terminach, jeśli klient poprosi o raport „ile laptopów mamy".
/// </para>
/// </remarks>
public sealed class MieniePowierzone : AuditableEntity<Guid>, ITenantScoped, IAuditable
{
    public Guid TenantId { get; private set; }

    public Guid EmployeeId { get; private set; }

    /// <summary>Kategoria wpisana ręcznie: „Laptop", „Telefon", „Klucze", „Odzież", „Narzędzia".</summary>
    public string Rodzaj { get; private set; } = null!;

    /// <summary>Co dokładnie: model, rozmiar, opis.</summary>
    public string Nazwa { get; private set; } = null!;

    /// <summary>Numer seryjny, inwentarzowy albo inny identyfikujący egzemplarz. Opcjonalny.</summary>
    public string? NumerSeryjny { get; private set; }

    /// <summary>Wartość w chwili wydania, jeśli firma ją ewidencjonuje. Opcjonalna.</summary>
    public decimal? Wartosc { get; private set; }

    public DateOnly WydanoDnia { get; private set; }

    /// <summary>Wypełnione = rzecz wróciła do firmy.</summary>
    public DateOnly? ZwroconoDnia { get; private set; }

    /// <summary>Kiedy pracownik potwierdził odbiór ze swojego konta. Puste = nie potwierdził.</summary>
    public DateTime? PotwierdzonoOdbior { get; private set; }

    public string? Notatka { get; private set; }

    private MieniePowierzone() { }

    public static MieniePowierzone Wydaj(
        Guid tenantId,
        Guid employeeId,
        string rodzaj,
        string nazwa,
        DateOnly wydanoDnia,
        string? numerSeryjny = null,
        decimal? wartosc = null,
        string? notatka = null)
    {
        if (string.IsNullOrWhiteSpace(rodzaj))
            throw new ArgumentException("Rodzaj jest wymagany.", nameof(rodzaj));
        if (string.IsNullOrWhiteSpace(nazwa))
            throw new ArgumentException("Nazwa jest wymagana.", nameof(nazwa));
        if (wartosc is < 0)
            throw new ArgumentException("Wartość nie może być ujemna.", nameof(wartosc));

        return new MieniePowierzone
        {
            TenantId = tenantId,
            EmployeeId = employeeId,
            Rodzaj = rodzaj.Trim(),
            Nazwa = nazwa.Trim(),
            NumerSeryjny = Oczysc(numerSeryjny),
            Wartosc = wartosc,
            WydanoDnia = wydanoDnia,
            Notatka = Oczysc(notatka),
        };
    }

    public void Zmien(string rodzaj, string nazwa, string? numerSeryjny, decimal? wartosc, string? notatka)
    {
        if (string.IsNullOrWhiteSpace(rodzaj))
            throw new ArgumentException("Rodzaj jest wymagany.", nameof(rodzaj));
        if (string.IsNullOrWhiteSpace(nazwa))
            throw new ArgumentException("Nazwa jest wymagana.", nameof(nazwa));
        if (wartosc is < 0)
            throw new ArgumentException("Wartość nie może być ujemna.", nameof(wartosc));

        Rodzaj = rodzaj.Trim();
        Nazwa = nazwa.Trim();
        NumerSeryjny = Oczysc(numerSeryjny);
        Wartosc = wartosc;
        Notatka = Oczysc(notatka);
    }

    public bool Zwrocone => ZwroconoDnia is not null;

    public void Zwroc(DateOnly zwroconoDnia, string? notatka)
    {
        if (Zwrocone)
            throw new InvalidOperationException("Ta rzecz została już zwrócona.");
        if (zwroconoDnia < WydanoDnia)
            throw new ArgumentException("Data zwrotu nie może być wcześniejsza niż data wydania.", nameof(zwroconoDnia));

        ZwroconoDnia = zwroconoDnia;
        if (!string.IsNullOrWhiteSpace(notatka))
            Notatka = Notatka is null ? notatka.Trim() : $"{Notatka}\nZwrot: {notatka.Trim()}";
    }

    /// <summary>Idempotentne: drugie potwierdzenie nie zmienia daty pierwszego.</summary>
    public void PotwierdzOdbior(DateTime teraz)
    {
        PotwierdzonoOdbior ??= teraz;
    }

    private static string? Oczysc(string? tekst) =>
        string.IsNullOrWhiteSpace(tekst) ? null : tekst.Trim();
}

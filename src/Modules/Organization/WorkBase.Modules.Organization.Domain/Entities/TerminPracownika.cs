using WorkBase.Shared.Domain;

namespace WorkBase.Modules.Organization.Domain.Entities;

/// <summary>Jak blisko jest termin — liczone względem dnia, nie przechowywane.</summary>
public enum StanTerminu
{
    /// <summary>Do terminu jest więcej czasu niż wyprzedzenie ustawione przy jego rodzaju.</summary>
    Aktualny = 0,

    /// <summary>Termin mieści się w oknie ostrzeżenia.</summary>
    Zbliza = 1,

    /// <summary>Termin już minął.</summary>
    Minal = 2,
}

/// <summary>
/// Konkretny termin przy pracowniku: data ważności badań, szkolenia, uprawnienia albo umowy.
/// </summary>
/// <remarks>
/// <para>
/// <b>Nic nie blokuje.</b> Minięty termin nie odbiera pracownikowi możliwości rejestracji czasu
/// ani składania wniosków — pokazujemy stan i zostawiamy decyzję firmie. Dopuszczenie do pracy
/// jest odpowiedzialnością pracodawcy.
/// </para>
/// <para>
/// Odnowienie zapisujemy jako NOWY termin, a stary zostaje z datą, którą miał. Dzięki temu
/// widać historię badań i szkoleń, a nie tylko ostatni stan — przy kontroli to jest różnica
/// między „mamy to udokumentowane" a „mamy tylko bieżącą datę".
/// </para>
/// </remarks>
public sealed class TerminPracownika : AuditableEntity<Guid>, ITenantScoped, IAuditable
{
    public Guid TenantId { get; private set; }

    public Guid EmployeeId { get; private set; }

    public Guid TypTerminuId { get; private set; }

    /// <summary>Data, do której termin obowiązuje.</summary>
    public DateOnly WaznyDo { get; private set; }

    /// <summary>Data wykonania badania, szkolenia albo zawarcia umowy. Opcjonalna.</summary>
    public DateOnly? WykonanyDnia { get; private set; }

    public string? Notatka { get; private set; }

    /// <summary>Skan zaświadczenia w module dokumentów, jeśli firma go dołączyła.</summary>
    public Guid? DokumentId { get; private set; }

    /// <summary>
    /// Termin zastąpiony nowszym. Nie kasujemy go, bo historia badań i szkoleń bywa potrzebna
    /// przy kontroli.
    /// </summary>
    public bool Archiwalny { get; private set; }

    private TerminPracownika() { }

    public static TerminPracownika Utworz(
        Guid tenantId,
        Guid employeeId,
        Guid typTerminuId,
        DateOnly waznyDo,
        DateOnly? wykonanyDnia = null,
        string? notatka = null,
        Guid? dokumentId = null)
    {
        if (wykonanyDnia is { } wykonany && wykonany > waznyDo)
        {
            throw new ArgumentException(
                "Data wykonania nie może być późniejsza niż data ważności.", nameof(wykonanyDnia));
        }

        return new TerminPracownika
        {
            TenantId = tenantId,
            EmployeeId = employeeId,
            TypTerminuId = typTerminuId,
            WaznyDo = waznyDo,
            WykonanyDnia = wykonanyDnia,
            Notatka = string.IsNullOrWhiteSpace(notatka) ? null : notatka.Trim(),
            DokumentId = dokumentId,
            Archiwalny = false,
        };
    }

    public void Zmien(DateOnly waznyDo, DateOnly? wykonanyDnia, string? notatka, Guid? dokumentId)
    {
        if (wykonanyDnia is { } wykonany && wykonany > waznyDo)
        {
            throw new ArgumentException(
                "Data wykonania nie może być późniejsza niż data ważności.", nameof(wykonanyDnia));
        }

        WaznyDo = waznyDo;
        WykonanyDnia = wykonanyDnia;
        Notatka = string.IsNullOrWhiteSpace(notatka) ? null : notatka.Trim();
        DokumentId = dokumentId;
    }

    public void Zarchiwizuj() => Archiwalny = true;

    /// <summary>
    /// Stan liczony względem podanego dnia i wyprzedzenia z rodzaju terminu. Nie zapisujemy go
    /// w bazie, bo zmienia się sam z upływem czasu — przechowywany szybko rozjechałby się
    /// z rzeczywistością i wymagał zadania, które tylko go odświeża.
    /// </summary>
    public StanTerminu Stan(DateOnly dzisiaj, int dniOstrzezenia)
    {
        if (WaznyDo < dzisiaj) return StanTerminu.Minal;
        return WaznyDo.DayNumber - dzisiaj.DayNumber <= dniOstrzezenia
            ? StanTerminu.Zbliza
            : StanTerminu.Aktualny;
    }
}

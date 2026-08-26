using WorkBase.Shared.Domain;

namespace WorkBase.Modules.Organization.Domain.Entities;

/// <summary>
/// Rodzaj terminu pilnowanego przy pracowniku — badania lekarskie, szkolenie BHP,
/// uprawnienie z datą ważności, koniec umowy.
/// </summary>
/// <remarks>
/// <para>
/// Słownik firmy, nie nasz. Nowa firma dostaje edytowalny zestaw startowy i może go zmienić,
/// usunąć albo rozszerzyć — tak samo jak typy urlopów czy statusy zadań. System nie wie z góry,
/// jakich terminów pilnuje dana branża, i nie ma prawa tego rozstrzygać.
/// </para>
/// <para>
/// <b>Termin niczego nie blokuje.</b> Pracownik z nieaktualnym badaniem normalnie zarejestruje
/// czas pracy i złoży wniosek. Pokazujemy, że termin minął, i zostawiamy decyzję firmie —
/// dopuszczenie do pracy jest odpowiedzialnością pracodawcy, a nie systemu.
/// </para>
/// </remarks>
public sealed class TypTerminu : AuditableEntity<Guid>, ITenantScoped, IAuditable
{
    public Guid TenantId { get; private set; }

    /// <summary>Kod używany przy zasiewie i imporcie; nazwę firma może dowolnie zmienić.</summary>
    public string Kod { get; private set; } = null!;

    public string Nazwa { get; private set; } = null!;

    public string? Opis { get; private set; }

    /// <summary>
    /// Ile dni przed upływem terminu ma się pojawić ostrzeżenie. Różne terminy mają różny
    /// czas reakcji: badania okresowe umawia się z tygodniowym wyprzedzeniem, a wypowiedzenie
    /// umowy wymaga miesięcy.
    /// </summary>
    public int DniOstrzezenia { get; private set; }

    public bool Aktywny { get; private set; }

    private TypTerminu() { }

    public static TypTerminu Utworz(
        Guid tenantId, string kod, string nazwa, string? opis, int dniOstrzezenia)
    {
        return new TypTerminu
        {
            TenantId = tenantId,
            Kod = Wymagany(kod, nameof(kod)),
            Nazwa = Wymagany(nazwa, nameof(nazwa)),
            Opis = string.IsNullOrWhiteSpace(opis) ? null : opis.Trim(),
            DniOstrzezenia = SprawdzDni(dniOstrzezenia),
            Aktywny = true,
        };
    }

    public void Zmien(string nazwa, string? opis, int dniOstrzezenia, bool aktywny)
    {
        Nazwa = Wymagany(nazwa, nameof(nazwa));
        Opis = string.IsNullOrWhiteSpace(opis) ? null : opis.Trim();
        DniOstrzezenia = SprawdzDni(dniOstrzezenia);
        Aktywny = aktywny;
    }

    private static string Wymagany(string wartosc, string nazwaPola) =>
        string.IsNullOrWhiteSpace(wartosc)
            ? throw new ArgumentException("Wartość jest wymagana.", nazwaPola)
            : wartosc.Trim();

    /// <summary>
    /// Górna granica to dwa lata: dłuższe wyprzedzenie znaczy, że ostrzeżenie wisi stale
    /// i przestaje cokolwiek znaczyć.
    /// </summary>
    private static int SprawdzDni(int dni) =>
        dni is < 0 or > 730
            ? throw new ArgumentOutOfRangeException(nameof(dni), "Wyprzedzenie musi mieścić się między 0 a 730 dniami.")
            : dni;
}

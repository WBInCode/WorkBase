using WorkBase.Shared.Domain;

namespace WorkBase.Modules.Organization.Domain.Entities;

/// <summary>Co uruchamia listę: przyjęcie nowej osoby albo jej odejście.</summary>
public enum WyzwalaczListy
{
    Przyjecie = 0,
    Pozegnanie = 1,
}

/// <summary>Kto dostaje zadanie z pozycji listy.</summary>
public enum WykonawcaPozycji
{
    /// <summary>Sam pracownik, którego dotyczy zdarzenie.</summary>
    Pracownik = 0,

    /// <summary>Jego aktualny przełożony ze struktury. Bez przełożonego pozycja jest pomijana.</summary>
    Przelozony = 1,

    /// <summary>Wskazana osoba — np. kadrowa, informatyk. Wymaga <c>OsobaId</c>.</summary>
    Osoba = 2,
}

/// <summary>
/// Szablon listy kontrolnej: przy przyjęciu albo odejściu pracownika sam zakłada zadania
/// z terminami i przypisaniem.
/// </summary>
/// <remarks>
/// <para>
/// Odpowiada na „co trzeba zrobić, gdy ktoś przychodzi / odchodzi" — pytanie, na które każda
/// firma ma odpowiedź w głowie jednej osoby albo w pliku, o którym nikt nie pamięta. Tu
/// odpowiedź jest zapisana raz i wykonuje się sama.
/// </para>
/// <para>
/// <b>Lista nieaktywna nic nie robi.</b> Nowa firma dostaje dwa przykłady wyłączone — do
/// obejrzenia i włączenia jednym kliknięciem, nie do zaskoczenia zadaniami, o które nie prosiła.
/// </para>
/// <para>
/// Pozycje nadpisujemy w całości przy zapisie: lista ma kilka wierszy i edytuje się ją jak
/// jeden formularz. Zadania już założone z poprzedniej wersji zostają — to zwykłe zadania.
/// </para>
/// </remarks>
public sealed class ListaKontrolna : AuditableEntity<Guid>, ITenantScoped, IAuditable
{
    private readonly List<PozycjaListyKontrolnej> _pozycje = [];

    public Guid TenantId { get; private set; }

    public string Nazwa { get; private set; } = null!;

    public WyzwalaczListy Wyzwalacz { get; private set; }

    public bool Aktywna { get; private set; }

    public IReadOnlyCollection<PozycjaListyKontrolnej> Pozycje => _pozycje.AsReadOnly();

    private ListaKontrolna() { }

    public static ListaKontrolna Utworz(Guid tenantId, string nazwa, WyzwalaczListy wyzwalacz, bool aktywna)
    {
        if (string.IsNullOrWhiteSpace(nazwa))
            throw new ArgumentException("Nazwa jest wymagana.", nameof(nazwa));

        return new ListaKontrolna
        {
            TenantId = tenantId,
            Nazwa = nazwa.Trim(),
            Wyzwalacz = wyzwalacz,
            Aktywna = aktywna,
        };
    }

    public void Zmien(string nazwa, WyzwalaczListy wyzwalacz, bool aktywna)
    {
        if (string.IsNullOrWhiteSpace(nazwa))
            throw new ArgumentException("Nazwa jest wymagana.", nameof(nazwa));

        Nazwa = nazwa.Trim();
        Wyzwalacz = wyzwalacz;
        Aktywna = aktywna;
    }

    /// <summary>Zastępuje wszystkie pozycje. Kolejność = kolejność na liście.</summary>
    public void UstawPozycje(IEnumerable<(string Tytul, int DniOdZdarzenia, WykonawcaPozycji Wykonawca, Guid? OsobaId)> pozycje)
    {
        var nowe = pozycje.ToList();
        if (nowe.Count == 0)
            throw new ArgumentException("Lista musi mieć co najmniej jedną pozycję.", nameof(pozycje));

        _pozycje.Clear();
        var kolejnosc = 0;
        foreach (var (tytul, dni, wykonawca, osobaId) in nowe)
            _pozycje.Add(PozycjaListyKontrolnej.Utworz(Id, tytul, dni, wykonawca, osobaId, kolejnosc++));
    }
}

public sealed class PozycjaListyKontrolnej : Entity<Guid>
{
    public Guid ListaId { get; private set; }

    public string Tytul { get; private set; } = null!;

    /// <summary>Termin zadania = data zdarzenia + tyle dni. 0 = tego samego dnia.</summary>
    public int DniOdZdarzenia { get; private set; }

    public WykonawcaPozycji Wykonawca { get; private set; }

    public Guid? OsobaId { get; private set; }

    public int Kolejnosc { get; private set; }

    private PozycjaListyKontrolnej() { }

    internal static PozycjaListyKontrolnej Utworz(
        Guid listaId, string tytul, int dniOdZdarzenia, WykonawcaPozycji wykonawca, Guid? osobaId, int kolejnosc)
    {
        if (string.IsNullOrWhiteSpace(tytul))
            throw new ArgumentException("Tytuł pozycji jest wymagany.", nameof(tytul));
        if (dniOdZdarzenia < 0)
            throw new ArgumentException("Dni od zdarzenia nie mogą być ujemne.", nameof(dniOdZdarzenia));
        if (wykonawca == WykonawcaPozycji.Osoba && osobaId is null)
            throw new ArgumentException("Pozycja przypisana do osoby musi ją wskazywać.", nameof(osobaId));

        return new PozycjaListyKontrolnej
        {
            ListaId = listaId,
            Tytul = tytul.Trim(),
            DniOdZdarzenia = dniOdZdarzenia,
            Wykonawca = wykonawca,
            OsobaId = wykonawca == WykonawcaPozycji.Osoba ? osobaId : null,
            Kolejnosc = kolejnosc,
        };
    }
}

using WorkBase.Shared.Domain;

namespace WorkBase.Modules.Organization.Domain.Entities;

/// <summary>
/// Okresowe zastepstwo w akceptacji wnioskow: na czas nieobecnosci przelozonego jego kolejke
/// przejmuje wskazana osoba.
/// </summary>
/// <remarks>
/// Bez tego urlop kierownika zatrzymywal wnioski calego zespolu — reguly eskalacji reaguja
/// dopiero na przekroczony termin, czyli na problem, ktory juz wystapil.
///
/// Zastepstwo celowo NIE przenosi uprawnien ani zakresu danych. Zmienia wylacznie to, kto jest
/// wskazywany jako akceptant nowo powstajacych wnioskow. Dzieki temu nie da sie przez nie
/// dostac do danych, do ktorych zastepca i tak nie ma prawa.
/// </remarks>
public sealed class Zastepstwo : AuditableEntity<Guid>, ITenantScoped, IAuditable
{
    public Guid TenantId { get; private set; }

    /// <summary>Osoba zastepowana — ta, ktora normalnie akceptuje.</summary>
    public Guid ZastepowanyEmployeeId { get; private set; }

    /// <summary>Osoba przejmujaca kolejke na czas nieobecnosci.</summary>
    public Guid ZastepcaEmployeeId { get; private set; }

    /// <summary>Pierwszy dzien obowiazywania (wlacznie).</summary>
    public DateOnly OdKiedy { get; private set; }

    /// <summary>Ostatni dzien obowiazywania (wlacznie).</summary>
    public DateOnly DoKiedy { get; private set; }

    public string? Powod { get; private set; }

    public bool Odwolane { get; private set; }

    private Zastepstwo() { }

    public static Result<Zastepstwo> Utworz(
        Guid tenantId,
        Guid zastepowanyEmployeeId,
        Guid zastepcaEmployeeId,
        DateOnly odKiedy,
        DateOnly doKiedy,
        string? powod = null)
    {
        if (zastepowanyEmployeeId == zastepcaEmployeeId)
        {
            return Result.Failure<Zastepstwo>(new Error(
                "Zastepstwo.SamSiebie",
                "Nie można wyznaczyć samego siebie na zastępcę."));
        }

        if (doKiedy < odKiedy)
        {
            return Result.Failure<Zastepstwo>(new Error(
                "Zastepstwo.ZlyZakres",
                "Data zakończenia zastępstwa jest wcześniejsza niż data rozpoczęcia."));
        }

        return new Zastepstwo
        {
            Id = Guid.CreateVersion7(),
            TenantId = tenantId,
            ZastepowanyEmployeeId = zastepowanyEmployeeId,
            ZastepcaEmployeeId = zastepcaEmployeeId,
            OdKiedy = odKiedy,
            DoKiedy = doKiedy,
            Powod = string.IsNullOrWhiteSpace(powod) ? null : powod.Trim(),
        };
    }

    /// <summary>Czy zastepstwo obowiazuje w danym dniu.</summary>
    public bool ObowiazujeW(DateOnly dzien)
        => !Odwolane && OdKiedy <= dzien && dzien <= DoKiedy;

    /// <summary>Czy zakres dni pokrywa sie z innym — sluzy do odrzucania nakladajacych sie wpisow.</summary>
    public bool NakladaSieZ(DateOnly odKiedy, DateOnly doKiedy)
        => !Odwolane && OdKiedy <= doKiedy && odKiedy <= DoKiedy;

    public void Odwolaj() => Odwolane = true;
}

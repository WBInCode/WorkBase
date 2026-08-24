using WorkBase.Shared.Domain;

namespace WorkBase.Modules.TimeTracking.Domain.Entities;

/// <summary>Skad wzial sie dzien wolny — rozroznienie potrzebne przy rozliczeniu.</summary>
public enum RodzajDniaWolnego
{
    /// <summary>Dzien ustawowo wolny od pracy.</summary>
    Swieto = 0,

    /// <summary>Dzien wolny ustalony przez firme — np. wolne za swieto wypadajace w sobote.</summary>
    Firmowy = 1,
}

/// <summary>
/// Dzien wolny w kalendarzu firmy.
/// </summary>
/// <remarks>
/// System celowo NIE zna z gory zadnych swiat i niczego nie narzuca. Firma dostaje przy
/// zalozeniu edytowalny zestaw startowy i moze go dowolnie zmienic, usunac albo rozszerzyc —
/// tak samo jak kazdy inny slownik. Produkt ma odwzorowywac regulamin firmy, a nie zastepowac
/// jej dzial kadr, i musi dzialac takze poza Polska.
///
/// Dzien wolny wplywa na dwie rzeczy: obniza norme czasu pracy w okresie i pozwala zastosowac
/// mnoznik do godzin przepracowanych w tym dniu.
/// </remarks>
public sealed class DzienWolny : AuditableEntity<Guid>, ITenantScoped, IAuditable
{
    public Guid TenantId { get; private set; }

    public DateOnly Data { get; private set; }

    public string Nazwa { get; private set; } = null!;

    public RodzajDniaWolnego Rodzaj { get; private set; }

    /// <summary>
    /// Czy dzien obniza norme czasu pracy. Rozdzielone od samego istnienia dnia, bo firma
    /// moze chciec oznaczyc dzien (np. wigilie) bez zmieniania normy.
    /// </summary>
    public bool ObnizaNorme { get; private set; }

    private DzienWolny() { }

    public static Result<DzienWolny> Utworz(
        Guid tenantId,
        DateOnly data,
        string nazwa,
        RodzajDniaWolnego rodzaj = RodzajDniaWolnego.Swieto,
        bool obnizaNorme = true)
    {
        if (string.IsNullOrWhiteSpace(nazwa))
        {
            return Result.Failure<DzienWolny>(new Error(
                "DzienWolny.BrakNazwy", "Dzień wolny musi mieć nazwę."));
        }

        return new DzienWolny
        {
            Id = Guid.CreateVersion7(),
            TenantId = tenantId,
            Data = data,
            Nazwa = nazwa.Trim(),
            Rodzaj = rodzaj,
            ObnizaNorme = obnizaNorme,
        };
    }

    public void Zmien(string nazwa, RodzajDniaWolnego rodzaj, bool obnizaNorme)
    {
        if (!string.IsNullOrWhiteSpace(nazwa)) Nazwa = nazwa.Trim();
        Rodzaj = rodzaj;
        ObnizaNorme = obnizaNorme;
    }
}

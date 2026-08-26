using WorkBase.Modules.Organization.Domain.Entities;

namespace WorkBase.Modules.Organization.Application.Contracts;

public interface ITypTerminuRepository
{
    /// <summary><paramref name="tylkoAktywne"/> = tak, jak widzi to pracownik; pełna lista dla administratora.</summary>
    Task<List<TypTerminu>> PobierzAsync(bool tylkoAktywne, CancellationToken cancellationToken = default);

    Task<TypTerminu?> PobierzAsync(Guid id, CancellationToken cancellationToken = default);

    Task<bool> KodIstniejeAsync(string kod, Guid? pomijajId = null, CancellationToken cancellationToken = default);

    Task DodajAsync(TypTerminu typ, CancellationToken cancellationToken = default);
}

/// <summary>Termin razem z rodzajem — bo stan liczy się z wyprzedzenia zapisanego przy rodzaju.</summary>
public sealed record TerminZRodzajem(TerminPracownika Termin, TypTerminu Typ);

/// <summary>
/// To samo plus dane osoby — dla listy zbiorczej, która pokazuje, KOMU kończy się termin.
/// Lista niesie nazwiska, więc po stronie serwera musi zostać zawężona zakresem danych.
/// </summary>
public sealed record TerminZOsoba(
    TerminPracownika Termin, TypTerminu Typ, string Imie, string Nazwisko);

public interface ITerminPracownikaRepository
{
    Task<List<TerminZRodzajem>> PobierzDlaPracownikaAsync(
        Guid employeeId, bool zArchiwalnymi, CancellationToken cancellationToken = default);

    /// <summary>
    /// Terminy, które minęły albo miną w ciągu <paramref name="wCiaguDni"/> dni. Filtrujemy po
    /// dacie w bazie, a nie po stanie w pamięci: stan zależy od wyprzedzenia rodzaju i liczy
    /// się dopiero po odczycie, więc filtr musi być szerszy niż ostateczny wynik.
    /// </summary>
    Task<List<TerminZOsoba>> PobierzWygasajaceAsync(
        DateOnly dzisiaj, int wCiaguDni, CancellationToken cancellationToken = default);

    Task<TerminPracownika?> PobierzAsync(Guid id, CancellationToken cancellationToken = default);

    Task DodajAsync(TerminPracownika termin, CancellationToken cancellationToken = default);
}

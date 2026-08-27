using WorkBase.Modules.Organization.Domain.Entities;

namespace WorkBase.Modules.Organization.Application.Contracts;

/// <summary>
/// Niezwrócona rzecz razem z osobą, która ją ma — dla listy „co do zwrotu". Lista niesie
/// nazwiska, więc po stronie serwera musi zostać zawężona zakresem danych.
/// </summary>
public sealed record MienieZOsoba(
    MieniePowierzone Mienie, string Imie, string Nazwisko, EmployeeStatus StatusPracownika, DateTime? TerminationDate);

public interface IMieniePowierzoneRepository
{
    Task<List<MieniePowierzone>> PobierzDlaPracownikaAsync(
        Guid employeeId, bool zeZwroconymi, CancellationToken cancellationToken = default);

    /// <summary>
    /// Wszystko, co nie wróciło, u osób nieaktywnych albo z ustawioną datą odejścia —
    /// czyli to, co ktoś powinien odebrać. Pracownicy aktywni bez daty odejścia nie trafiają
    /// tu celowo: laptop u kogoś, kto pracuje, nie jest „do zwrotu".
    /// </summary>
    Task<List<MienieZOsoba>> PobierzDoZwrotuAsync(CancellationToken cancellationToken = default);

    /// <summary>Ile niezwróconych rzeczy ma pracownik — do ostrzeżenia przy dezaktywacji.</summary>
    Task<int> PoliczNiezwroconeAsync(Guid employeeId, CancellationToken cancellationToken = default);

    Task<MieniePowierzone?> PobierzAsync(Guid id, CancellationToken cancellationToken = default);

    Task DodajAsync(MieniePowierzone mienie, CancellationToken cancellationToken = default);
}

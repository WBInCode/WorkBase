namespace WorkBase.Modules.Dashboard.Application.Dtos;

/// <summary>Jedna pozycja wymagająca uwagi — konkretna osoba albo rzecz.</summary>
public sealed record PozycjaAlertuDto(Guid Id, string Opis);

/// <summary>
/// Alert na pulpicie: co wymaga uwagi, ilu rzeczy dotyczy i gdzie się tym zająć.
/// </summary>
/// <param name="Waga">
/// <c>pilne</c> — coś stanęło albo ktoś czeka; <c>uwaga</c> — do uzupełnienia, nie blokuje.
/// Rozdzielone od liczby, bo dziesięć brakujących stawek to nie to samo co jeden stojący wniosek.
/// </param>
/// <param name="Pozycje">Kilka pierwszych pozycji do pokazania wprost — reszta zostaje w liczbie.</param>
public sealed record AlertDto(
    string Kod,
    string Waga,
    string Tytul,
    string Opis,
    int Liczba,
    string? Sciezka,
    IReadOnlyList<PozycjaAlertuDto> Pozycje);

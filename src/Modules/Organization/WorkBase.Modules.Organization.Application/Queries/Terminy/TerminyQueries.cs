using WorkBase.Modules.Organization.Application.Commands.Terminy;
using WorkBase.Modules.Organization.Application.Contracts;
using WorkBase.Modules.Organization.Domain.Entities;
using WorkBase.Shared.Cqrs;
using WorkBase.Shared.Domain;

namespace WorkBase.Modules.Organization.Application.Queries.Terminy;

public sealed record PobierzTypyTerminowQuery(bool Wszystkie = false)
    : IQuery<List<TypTerminuDto>>, ITenantRequest
{
    public Guid TenantId { get; set; }
}

public sealed class PobierzTypyTerminowHandler(ITypTerminuRepository typy)
    : IQueryHandler<PobierzTypyTerminowQuery, List<TypTerminuDto>>
{
    public async Task<Result<List<TypTerminuDto>>> Handle(PobierzTypyTerminowQuery request, CancellationToken ct)
    {
        var lista = await typy.PobierzAsync(tylkoAktywne: !request.Wszystkie, ct);
        return lista
            .Select(t => new TypTerminuDto(t.Id, t.Kod, t.Nazwa, t.Opis, t.DniOstrzezenia, t.Aktywny))
            .ToList();
    }
}

public sealed record PobierzTerminyPracownikaQuery(Guid EmployeeId, bool ZArchiwalnymi = false)
    : IQuery<List<TerminDto>>, ITenantRequest
{
    public Guid TenantId { get; set; }
}

public sealed class PobierzTerminyPracownikaHandler(ITerminPracownikaRepository terminy)
    : IQueryHandler<PobierzTerminyPracownikaQuery, List<TerminDto>>
{
    public async Task<Result<List<TerminDto>>> Handle(PobierzTerminyPracownikaQuery request, CancellationToken ct)
    {
        var dzisiaj = DateOnly.FromDateTime(DateTime.UtcNow);
        var lista = await terminy.PobierzDlaPracownikaAsync(request.EmployeeId, request.ZArchiwalnymi, ct);

        return lista.Select(x => new TerminDto(
            x.Termin.Id,
            x.Termin.EmployeeId,
            x.Typ.Id,
            x.Typ.Nazwa,
            x.Termin.WaznyDo,
            x.Termin.WykonanyDnia,
            x.Termin.Notatka,
            x.Termin.DokumentId,
            x.Termin.Archiwalny,
            x.Termin.Stan(dzisiaj, x.Typ.DniOstrzezenia).ToString(),
            x.Termin.WaznyDo.DayNumber - dzisiaj.DayNumber)).ToList();
    }
}

/// <summary>
/// „Co wygasa w najbliższych N dniach” — lista zbiorcza dla kadr i przełożonych.
/// </summary>
/// <remarks>
/// Zwraca tylko terminy, które realnie wymagają uwagi: minione albo mieszczące się w oknie
/// ostrzeżenia swojego rodzaju. Zapytanie do bazy filtruje szerzej (po samej dacie), bo okno
/// zależy od rodzaju i da się je zastosować dopiero po odczycie.
///
/// Lista niesie nazwiska, więc zawężenie zakresem danych robi endpoint — tak samo jak przy
/// panelu „co wymaga uwagi”.
/// </remarks>
public sealed record PobierzWygasajaceTerminyQuery(int WCiaguDni = 30)
    : IQuery<List<WygasajacyTerminDto>>, ITenantRequest
{
    public Guid TenantId { get; set; }
}

public sealed class PobierzWygasajaceTerminyHandler(ITerminPracownikaRepository terminy)
    : IQueryHandler<PobierzWygasajaceTerminyQuery, List<WygasajacyTerminDto>>
{
    public async Task<Result<List<WygasajacyTerminDto>>> Handle(
        PobierzWygasajaceTerminyQuery request, CancellationToken ct)
    {
        var dni = Math.Clamp(request.WCiaguDni, 0, 365);
        var dzisiaj = DateOnly.FromDateTime(DateTime.UtcNow);
        var lista = await terminy.PobierzWygasajaceAsync(dzisiaj, dni, ct);

        return lista
            .Select(x => new
            {
                x.Termin,
                x.Typ,
                Osoba = $"{x.Imie} {x.Nazwisko}",
                Stan = x.Termin.Stan(dzisiaj, x.Typ.DniOstrzezenia),
            })
            .Where(x => x.Stan != StanTerminu.Aktualny)
            .Select(x => new WygasajacyTerminDto(
                x.Termin.Id,
                x.Termin.EmployeeId,
                x.Osoba,
                x.Typ.Nazwa,
                x.Termin.WaznyDo,
                x.Stan.ToString(),
                x.Termin.WaznyDo.DayNumber - dzisiaj.DayNumber))
            .ToList();
    }
}

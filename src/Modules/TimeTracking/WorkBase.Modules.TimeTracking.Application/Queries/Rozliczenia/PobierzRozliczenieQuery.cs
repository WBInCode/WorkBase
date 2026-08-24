using WorkBase.Modules.TimeTracking.Application.Contracts;
using WorkBase.Modules.TimeTracking.Domain.Services;
using WorkBase.Shared.Cqrs;
using WorkBase.Shared.Domain;

namespace WorkBase.Modules.TimeTracking.Application.Queries.Rozliczenia;

/// <summary>Rozliczenie jednego pracownika za okres.</summary>
public sealed record WierszRozliczeniaDto(
    Guid EmployeeId,
    decimal NormaH,
    decimal PrzepracowaneH,
    decimal ZwykleH,
    decimal NadgodzinyH,
    decimal NocneH,
    decimal SwiateczneH,
    decimal Zasadnicze,
    decimal ZaNadgodziny,
    decimal DodatekNocny,
    decimal DodatekSwiateczny,
    decimal Razem);

/// <summary>Stawki pracownikow podaje wolajacy — modul czasu pracy nie zna kartoteki kadrowej.</summary>
public sealed record PobierzRozliczenieQuery(
    DateOnly Od,
    DateOnly Do,
    IReadOnlyDictionary<Guid, decimal> StawkiPracownikow,
    decimal MnoznikNadgodzin,
    decimal MnoznikNocny,
    decimal MnoznikSwiateczny,
    TimeOnly NocOd,
    TimeOnly NocDo) : IQuery<List<WierszRozliczeniaDto>>, ITenantRequest
{
    public Guid TenantId { get; set; }
}

/// <summary>
/// Liczy rozliczenie po stronie serwera.
/// </summary>
/// <remarks>
/// Wczesniej cale wyliczenie robil sie w przegladarce z sumy godzin na karcie czasu. Przez to
/// nie dalo sie zastosowac dodatku nocnego (potrzebne sa wpisy, nie suma) ani swiatecznego
/// (potrzebny kalendarz dni wolnych), a samego wzoru nie dalo sie przetestowac.
///
/// Godziny licza sie doba po dobie, bo tylko wtedy zmiana nocna dzieli sie miedzy dni tak samo,
/// jak przy ewidencji czasu pracy.
/// </remarks>
public sealed class PobierzRozliczenieHandler(
    ITimeEntryRepository wpisy,
    IScheduleRepository grafiki,
    IDzienWolnyRepository dniWolne)
    : IQueryHandler<PobierzRozliczenieQuery, List<WierszRozliczeniaDto>>
{
    public async Task<Result<List<WierszRozliczeniaDto>>> Handle(
        PobierzRozliczenieQuery request, CancellationToken cancellationToken)
    {
        var pracownicy = request.StawkiPracownikow.Keys.ToList();
        if (pracownicy.Count == 0) return new List<WierszRozliczeniaDto>();

        var wszystkieWpisy = await wpisy.GetEntriesForEmployeesRangeAsync(
            request.TenantId, pracownicy, request.Od, request.Do, cancellationToken);

        var wszystkieGrafiki = await grafiki.GetByEmployeesDateRangeAsync(
            request.TenantId, pracownicy, request.Od, request.Do, cancellationToken);

        var wolne = await dniWolne.PobierzZakresAsync(
            request.TenantId, request.Od, request.Do, cancellationToken);
        var dniWolneDaty = wolne.Select(d => d.Data).ToHashSet();

        var teraz = DateTime.UtcNow;
        var wpisyPerPracownik = wszystkieWpisy.GroupBy(e => e.EmployeeId)
            .ToDictionary(g => g.Key, g => g.ToList());
        var grafikiPerPracownik = wszystkieGrafiki.GroupBy(s => s.EmployeeId)
            .ToDictionary(g => g.Key, g => g.ToList());

        var wynik = new List<WierszRozliczeniaDto>();

        foreach (var (employeeId, stawka) in request.StawkiPracownikow)
        {
            var moje = wpisyPerPracownik.TryGetValue(employeeId, out var w) ? w : [];
            var mojeGrafiki = grafikiPerPracownik.TryGetValue(employeeId, out var g) ? g : [];

            decimal przepracowaneH = 0, nocneH = 0, swiateczneH = 0;

            for (var dzien = request.Od; dzien <= request.Do; dzien = dzien.AddDays(1))
            {
                var czas = WorkedTimeCalculator.ForDate(moje, dzien, teraz);
                var netto = (decimal)czas.Net.TotalHours;
                przepracowaneH += netto;

                nocneH += (decimal)WorkedTimeCalculator
                    .GodzinyNocneWDobie(moje, dzien, request.NocOd, request.NocDo, teraz)
                    .TotalHours;

                if (dniWolneDaty.Contains(dzien)) swiateczneH += netto;
            }

            // Norme bierzemy z grafiku, ale dni wolne oznaczone jako obnizajace norme z niej
            // wypadaja — inaczej swieto podnosiloby norme i zjadalo nadgodziny.
            var normaH = mojeGrafiki
                .Where(s => s.Date >= request.Od && s.Date <= request.Do)
                .Where(s => !wolne.Any(d => d.Data == s.Date && d.ObnizaNorme))
                .Sum(s => (decimal)s.PlannedDuration.TotalHours);

            var kwoty = RozliczenieCalculator.Policz(
                new SkladnikiCzasu(normaH, przepracowaneH, nocneH, swiateczneH),
                new StawkiRozliczenia(stawka, request.MnoznikNadgodzin, request.MnoznikNocny, request.MnoznikSwiateczny));

            wynik.Add(new WierszRozliczeniaDto(
                employeeId,
                normaH,
                przepracowaneH,
                kwoty.ZwykleH,
                kwoty.NadgodzinyH,
                nocneH,
                swiateczneH,
                kwoty.Zasadnicze,
                kwoty.ZaNadgodziny,
                kwoty.DodatekNocny,
                kwoty.DodatekSwiateczny,
                kwoty.Razem));
        }

        return wynik;
    }
}

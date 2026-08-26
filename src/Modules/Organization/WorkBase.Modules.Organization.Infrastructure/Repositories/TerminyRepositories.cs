using Microsoft.EntityFrameworkCore;
using WorkBase.Infrastructure.Persistence;
using WorkBase.Modules.Organization.Application.Contracts;
using WorkBase.Modules.Organization.Domain.Entities;

namespace WorkBase.Modules.Organization.Infrastructure.Repositories;

public sealed class TypTerminuRepository(WorkBaseDbContext dbContext) : ITypTerminuRepository
{
    public async Task<List<TypTerminu>> PobierzAsync(
        bool tylkoAktywne, CancellationToken cancellationToken = default)
    {
        var zapytanie = dbContext.Set<TypTerminu>().AsQueryable();
        if (tylkoAktywne) zapytanie = zapytanie.Where(t => t.Aktywny);

        return await zapytanie.OrderBy(t => t.Nazwa).ToListAsync(cancellationToken);
    }

    public Task<TypTerminu?> PobierzAsync(Guid id, CancellationToken cancellationToken = default) =>
        dbContext.Set<TypTerminu>().FirstOrDefaultAsync(t => t.Id == id, cancellationToken);

    public Task<bool> KodIstniejeAsync(
        string kod, Guid? pomijajId = null, CancellationToken cancellationToken = default)
    {
        var znormalizowany = kod.Trim().ToUpperInvariant();
        return dbContext.Set<TypTerminu>()
            .AnyAsync(
                t => t.Kod.ToUpper() == znormalizowany && (pomijajId == null || t.Id != pomijajId),
                cancellationToken);
    }

    public async Task DodajAsync(TypTerminu typ, CancellationToken cancellationToken = default) =>
        await dbContext.Set<TypTerminu>().AddAsync(typ, cancellationToken);
}

public sealed class TerminPracownikaRepository(WorkBaseDbContext dbContext) : ITerminPracownikaRepository
{
    public async Task<List<TerminZRodzajem>> PobierzDlaPracownikaAsync(
        Guid employeeId, bool zArchiwalnymi, CancellationToken cancellationToken = default)
    {
        var zapytanie = dbContext.Set<TerminPracownika>().Where(t => t.EmployeeId == employeeId);
        if (!zArchiwalnymi) zapytanie = zapytanie.Where(t => !t.Archiwalny);

        return await Polacz(zapytanie).OrderBy(x => x.Termin.WaznyDo).ToListAsync(cancellationToken);
    }

    public async Task<List<TerminZOsoba>> PobierzWygasajaceAsync(
        DateOnly dzisiaj, int wCiaguDni, CancellationToken cancellationToken = default)
    {
        var granica = dzisiaj.AddDays(wCiaguDni);

        return await dbContext.Set<TerminPracownika>()
            .Where(t => !t.Archiwalny && t.WaznyDo <= granica)
            .Join(dbContext.Set<TypTerminu>(), t => t.TypTerminuId, typ => typ.Id,
                (t, typ) => new { Termin = t, Typ = typ })
            .Join(dbContext.Set<Employee>(), x => x.Termin.EmployeeId, e => e.Id,
                (x, e) => new TerminZOsoba(x.Termin, x.Typ, e.FirstName, e.LastName))
            .OrderBy(x => x.Termin.WaznyDo)
            .ToListAsync(cancellationToken);
    }

    public Task<TerminPracownika?> PobierzAsync(Guid id, CancellationToken cancellationToken = default) =>
        dbContext.Set<TerminPracownika>().FirstOrDefaultAsync(t => t.Id == id, cancellationToken);

    public async Task DodajAsync(TerminPracownika termin, CancellationToken cancellationToken = default) =>
        await dbContext.Set<TerminPracownika>().AddAsync(termin, cancellationToken);

    /// <summary>
    /// Złączenie po identyfikatorze rodzaju zamiast właściwości nawigacyjnej: encje modułu nie
    /// deklarują relacji między sobą, żeby nie wciągać całego grafu przy każdym odczycie.
    /// </summary>
    private IQueryable<TerminZRodzajem> Polacz(IQueryable<TerminPracownika> zapytanie) =>
        zapytanie.Join(
            dbContext.Set<TypTerminu>(),
            termin => termin.TypTerminuId,
            typ => typ.Id,
            (termin, typ) => new TerminZRodzajem(termin, typ));
}

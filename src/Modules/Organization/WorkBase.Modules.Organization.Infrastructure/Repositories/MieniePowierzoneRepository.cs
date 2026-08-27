using Microsoft.EntityFrameworkCore;
using WorkBase.Infrastructure.Persistence;
using WorkBase.Modules.Organization.Application.Contracts;
using WorkBase.Modules.Organization.Domain.Entities;

namespace WorkBase.Modules.Organization.Infrastructure.Repositories;

public sealed class MieniePowierzoneRepository(WorkBaseDbContext dbContext) : IMieniePowierzoneRepository
{
    public async Task<List<MieniePowierzone>> PobierzDlaPracownikaAsync(
        Guid employeeId, bool zeZwroconymi, CancellationToken cancellationToken = default)
    {
        var zapytanie = dbContext.Set<MieniePowierzone>().Where(m => m.EmployeeId == employeeId);
        if (!zeZwroconymi) zapytanie = zapytanie.Where(m => m.ZwroconoDnia == null);

        // Niezwrocone na gorze, potem od najnowszego wydania — tak czyta to kadrowa.
        return await zapytanie
            .OrderBy(m => m.ZwroconoDnia != null)
            .ThenByDescending(m => m.WydanoDnia)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<MienieZOsoba>> PobierzDoZwrotuAsync(CancellationToken cancellationToken = default)
    {
        return await dbContext.Set<MieniePowierzone>()
            .Where(m => m.ZwroconoDnia == null)
            .Join(dbContext.Set<Employee>(), m => m.EmployeeId, e => e.Id,
                (m, e) => new { Mienie = m, Pracownik = e })
            .Where(x => x.Pracownik.Status == EmployeeStatus.Inactive || x.Pracownik.TerminationDate != null)
            .OrderBy(x => x.Pracownik.LastName).ThenBy(x => x.Pracownik.FirstName)
            .ThenBy(x => x.Mienie.Rodzaj)
            .Select(x => new MienieZOsoba(
                x.Mienie, x.Pracownik.FirstName, x.Pracownik.LastName,
                x.Pracownik.Status, x.Pracownik.TerminationDate))
            .ToListAsync(cancellationToken);
    }

    public Task<int> PoliczNiezwroconeAsync(Guid employeeId, CancellationToken cancellationToken = default) =>
        dbContext.Set<MieniePowierzone>()
            .CountAsync(m => m.EmployeeId == employeeId && m.ZwroconoDnia == null, cancellationToken);

    public Task<MieniePowierzone?> PobierzAsync(Guid id, CancellationToken cancellationToken = default) =>
        dbContext.Set<MieniePowierzone>().FirstOrDefaultAsync(m => m.Id == id, cancellationToken);

    public async Task DodajAsync(MieniePowierzone mienie, CancellationToken cancellationToken = default) =>
        await dbContext.Set<MieniePowierzone>().AddAsync(mienie, cancellationToken);
}

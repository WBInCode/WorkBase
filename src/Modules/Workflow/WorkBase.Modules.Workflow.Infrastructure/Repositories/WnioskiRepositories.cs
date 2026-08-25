using Microsoft.EntityFrameworkCore;
using WorkBase.Infrastructure.Persistence;
using WorkBase.Modules.Workflow.Application.Contracts;
using WorkBase.Modules.Workflow.Domain.Entities;

namespace WorkBase.Modules.Workflow.Infrastructure.Repositories;

public sealed class TypWnioskuRepository(WorkBaseDbContext dbContext) : ITypWnioskuRepository
{
    public async Task<List<TypWniosku>> PobierzWszystkieAsync(
        Guid tenantId, bool tylkoAktywne, CancellationToken ct = default)
    {
        var zapytanie = dbContext.Set<TypWniosku>().Where(t => t.TenantId == tenantId);
        if (tylkoAktywne) zapytanie = zapytanie.Where(t => t.Aktywny);

        return await zapytanie.OrderBy(t => t.Nazwa).ToListAsync(ct);
    }

    public async Task<TypWniosku?> PobierzAsync(Guid tenantId, Guid id, CancellationToken ct = default)
        => await dbContext.Set<TypWniosku>().FirstOrDefaultAsync(t => t.TenantId == tenantId && t.Id == id, ct);

    public async Task<bool> IstniejeKodAsync(Guid tenantId, string kod, CancellationToken ct = default)
        => await dbContext.Set<TypWniosku>()
            .AnyAsync(t => t.TenantId == tenantId && t.Kod == kod.Trim().ToUpper(), ct);

    public async Task DodajAsync(TypWniosku typ, CancellationToken ct = default)
        => await dbContext.Set<TypWniosku>().AddAsync(typ, ct);
}

public sealed class WnioskiRepository(WorkBaseDbContext dbContext) : IWnioskiRepository
{
    public async Task<List<Wniosek>> PobierzPracownikaAsync(
        Guid tenantId, Guid employeeId, CancellationToken ct = default)
        => await dbContext.Set<Wniosek>()
            .Where(w => w.TenantId == tenantId && w.EmployeeId == employeeId)
            .OrderByDescending(w => w.ZlozonyO)
            .ToListAsync(ct);

    public async Task<Wniosek?> PobierzAsync(Guid tenantId, Guid id, CancellationToken ct = default)
        => await dbContext.Set<Wniosek>().FirstOrDefaultAsync(w => w.TenantId == tenantId && w.Id == id, ct);

    public async Task DodajAsync(Wniosek wniosek, CancellationToken ct = default)
        => await dbContext.Set<Wniosek>().AddAsync(wniosek, ct);
}

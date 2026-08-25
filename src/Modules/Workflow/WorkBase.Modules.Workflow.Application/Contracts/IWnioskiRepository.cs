using WorkBase.Modules.Workflow.Domain.Entities;

namespace WorkBase.Modules.Workflow.Application.Contracts;

public interface ITypWnioskuRepository
{
    Task<List<TypWniosku>> PobierzWszystkieAsync(Guid tenantId, bool tylkoAktywne, CancellationToken ct = default);
    Task<TypWniosku?> PobierzAsync(Guid tenantId, Guid id, CancellationToken ct = default);
    Task<bool> IstniejeKodAsync(Guid tenantId, string kod, CancellationToken ct = default);
    Task DodajAsync(TypWniosku typ, CancellationToken ct = default);
}

public interface IWnioskiRepository
{
    Task<List<Wniosek>> PobierzPracownikaAsync(Guid tenantId, Guid employeeId, CancellationToken ct = default);
    Task<Wniosek?> PobierzAsync(Guid tenantId, Guid id, CancellationToken ct = default);
    Task DodajAsync(Wniosek wniosek, CancellationToken ct = default);
}

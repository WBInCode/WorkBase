using Microsoft.EntityFrameworkCore;
using WorkBase.Infrastructure.Persistence;
using WorkBase.Modules.Organization.Application.Contracts;
using WorkBase.Modules.Organization.Domain.Entities;

namespace WorkBase.Modules.Organization.Infrastructure.Repositories;

public sealed class ListaKontrolnaRepository(WorkBaseDbContext dbContext) : IListaKontrolnaRepository
{
    public Task<List<ListaKontrolna>> PobierzWszystkieAsync(CancellationToken cancellationToken = default) =>
        dbContext.Set<ListaKontrolna>().ToListAsync(cancellationToken);

    public Task<ListaKontrolna?> PobierzAsync(Guid id, CancellationToken cancellationToken = default) =>
        dbContext.Set<ListaKontrolna>().FirstOrDefaultAsync(l => l.Id == id, cancellationToken);

    public async Task DodajAsync(ListaKontrolna lista, CancellationToken cancellationToken = default) =>
        await dbContext.Set<ListaKontrolna>().AddAsync(lista, cancellationToken);
}

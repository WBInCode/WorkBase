using Microsoft.EntityFrameworkCore;
using WorkBase.Infrastructure.Persistence;
using WorkBase.Modules.Organization.Application.Contracts;
using WorkBase.Modules.Organization.Domain.Entities;

namespace WorkBase.Modules.Organization.Infrastructure.Repositories;

public sealed class ZastepstwoRepository(WorkBaseDbContext dbContext) : IZastepstwoRepository
{
    public async Task<Zastepstwo?> PobierzObowiazujaceAsync(
        Guid zastepowanyEmployeeId, DateOnly dzien, CancellationToken cancellationToken = default)
    {
        return await dbContext.Set<Zastepstwo>()
            .Where(z => z.ZastepowanyEmployeeId == zastepowanyEmployeeId
                        && !z.Odwolane
                        && z.OdKiedy <= dzien
                        && dzien <= z.DoKiedy)
            .OrderBy(z => z.OdKiedy)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<List<Zastepstwo>> PobierzDlaOsobyAsync(
        Guid zastepowanyEmployeeId, CancellationToken cancellationToken = default)
    {
        return await dbContext.Set<Zastepstwo>()
            .Where(z => z.ZastepowanyEmployeeId == zastepowanyEmployeeId && !z.Odwolane)
            .OrderByDescending(z => z.OdKiedy)
            .ToListAsync(cancellationToken);
    }

    public async Task<Zastepstwo?> PobierzAsync(Guid id, CancellationToken cancellationToken = default)
        => await dbContext.Set<Zastepstwo>().FirstOrDefaultAsync(z => z.Id == id, cancellationToken);

    public async Task DodajAsync(Zastepstwo zastepstwo, CancellationToken cancellationToken = default)
        => await dbContext.Set<Zastepstwo>().AddAsync(zastepstwo, cancellationToken);
}

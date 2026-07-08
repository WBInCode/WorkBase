using Microsoft.EntityFrameworkCore;
using WorkBase.Infrastructure.Persistence;
using WorkBase.Modules.Organization.Application.Contracts;
using WorkBase.Modules.Organization.Domain.Entities;

namespace WorkBase.Modules.Organization.Infrastructure.Repositories;

public sealed class TenantRepository(WorkBaseDbContext dbContext) : ITenantRepository
{
    public async Task<List<Tenant>> GetAllAsync(CancellationToken cancellationToken = default)
        => await dbContext.Set<Tenant>()
            .AsNoTracking()
            .OrderBy(t => t.Name)
            .ToListAsync(cancellationToken);

    public async Task<Tenant?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => await dbContext.Set<Tenant>()
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == id, cancellationToken);
}

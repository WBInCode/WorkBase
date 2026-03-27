using Microsoft.EntityFrameworkCore;
using WorkBase.Infrastructure.Persistence;
using WorkBase.Modules.TimeTracking.Application.Contracts;
using WorkBase.Modules.TimeTracking.Domain.Entities;

namespace WorkBase.Modules.TimeTracking.Infrastructure.Repositories;

public sealed class ScheduleTemplateRepository(WorkBaseDbContext dbContext) : IScheduleTemplateRepository
{
    public async Task<ScheduleTemplate?> GetByIdAsync(
        Guid tenantId, Guid id, CancellationToken cancellationToken = default)
    {
        return await dbContext.Set<ScheduleTemplate>()
            .FirstOrDefaultAsync(t => t.TenantId == tenantId && t.Id == id, cancellationToken);
    }

    public async Task<List<ScheduleTemplate>> GetAllAsync(
        Guid tenantId, CancellationToken cancellationToken = default)
    {
        return await dbContext.Set<ScheduleTemplate>()
            .Where(t => t.TenantId == tenantId)
            .OrderBy(t => t.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> NameExistsAsync(
        Guid tenantId, string name, Guid? excludeId = null, CancellationToken cancellationToken = default)
    {
        return await dbContext.Set<ScheduleTemplate>()
            .AnyAsync(t =>
                t.TenantId == tenantId
                && t.Name == name
                && (excludeId == null || t.Id != excludeId),
            cancellationToken);
    }

    public async Task AddAsync(ScheduleTemplate template, CancellationToken cancellationToken = default)
    {
        await dbContext.Set<ScheduleTemplate>().AddAsync(template, cancellationToken);
    }

    public void Update(ScheduleTemplate template)
    {
        dbContext.Set<ScheduleTemplate>().Update(template);
    }
}

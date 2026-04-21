using Microsoft.EntityFrameworkCore;
using WorkBase.Infrastructure.Persistence;
using WorkBase.Modules.Sales.Application.Contracts;
using WorkBase.Modules.Sales.Domain.Entities;

namespace WorkBase.Modules.Sales.Infrastructure.Repositories;

public sealed class LeadRepository(WorkBaseDbContext db) : ILeadRepository
{
    public async Task<Lead?> GetByIdAsync(Guid id, CancellationToken ct) => await db.Set<Lead>().FindAsync([id], ct);
    public async Task<List<Lead>> GetAllAsync(Guid tenantId, CancellationToken ct) =>
        await db.Set<Lead>().Where(l => l.TenantId == tenantId).OrderByDescending(l => l.CreatedAt).ToListAsync(ct);
    public async Task AddAsync(Lead lead, CancellationToken ct) => await db.Set<Lead>().AddAsync(lead, ct);
    public void Update(Lead lead) => db.Set<Lead>().Update(lead);
}

public sealed class OpportunityRepository(WorkBaseDbContext db) : IOpportunityRepository
{
    public async Task<Opportunity?> GetByIdAsync(Guid id, CancellationToken ct) => await db.Set<Opportunity>().FindAsync([id], ct);
    public async Task<List<Opportunity>> GetAllAsync(Guid tenantId, CancellationToken ct) =>
        await db.Set<Opportunity>().Where(o => o.TenantId == tenantId).OrderByDescending(o => o.CreatedAt).ToListAsync(ct);
    public async Task<List<Opportunity>> GetByStageAsync(Guid tenantId, DealStage stage, CancellationToken ct) =>
        await db.Set<Opportunity>().Where(o => o.TenantId == tenantId && o.Stage == stage).ToListAsync(ct);
    public async Task AddAsync(Opportunity opp, CancellationToken ct) => await db.Set<Opportunity>().AddAsync(opp, ct);
    public void Update(Opportunity opp) => db.Set<Opportunity>().Update(opp);
}

public sealed class OfferRepository(WorkBaseDbContext db) : IOfferRepository
{
    public async Task<Offer?> GetByIdAsync(Guid id, CancellationToken ct) => await db.Set<Offer>().FindAsync([id], ct);
    public async Task<List<Offer>> GetByOpportunityAsync(Guid opportunityId, CancellationToken ct) =>
        await db.Set<Offer>().Where(o => o.OpportunityId == opportunityId).OrderByDescending(o => o.CreatedAt).ToListAsync(ct);
    public async Task AddAsync(Offer offer, CancellationToken ct) => await db.Set<Offer>().AddAsync(offer, ct);
    public void Update(Offer offer) => db.Set<Offer>().Update(offer);
}

public sealed class PipelineStageRepository(WorkBaseDbContext db) : IPipelineStageRepository
{
    public async Task<List<PipelineStage>> GetAllAsync(Guid tenantId, CancellationToken ct) =>
        await db.Set<PipelineStage>().Where(s => s.TenantId == tenantId).OrderBy(s => s.SortOrder).ToListAsync(ct);
    public async Task<PipelineStage?> GetByIdAsync(Guid id, CancellationToken ct) => await db.Set<PipelineStage>().FindAsync([id], ct);
    public async Task AddAsync(PipelineStage stage, CancellationToken ct) => await db.Set<PipelineStage>().AddAsync(stage, ct);
    public void Update(PipelineStage stage) => db.Set<PipelineStage>().Update(stage);
    public void Delete(PipelineStage stage) => db.Set<PipelineStage>().Remove(stage);
}

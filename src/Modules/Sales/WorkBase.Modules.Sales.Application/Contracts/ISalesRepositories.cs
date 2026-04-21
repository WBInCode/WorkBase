using WorkBase.Modules.Sales.Domain.Entities;

namespace WorkBase.Modules.Sales.Application.Contracts;

public interface ILeadRepository
{
    Task<Lead?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<List<Lead>> GetAllAsync(Guid tenantId, CancellationToken ct = default);
    Task AddAsync(Lead lead, CancellationToken ct = default);
    void Update(Lead lead);
}

public interface IOpportunityRepository
{
    Task<Opportunity?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<List<Opportunity>> GetAllAsync(Guid tenantId, CancellationToken ct = default);
    Task<List<Opportunity>> GetByStageAsync(Guid tenantId, DealStage stage, CancellationToken ct = default);
    Task AddAsync(Opportunity opportunity, CancellationToken ct = default);
    void Update(Opportunity opportunity);
}

public interface IOfferRepository
{
    Task<Offer?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<List<Offer>> GetByOpportunityAsync(Guid opportunityId, CancellationToken ct = default);
    Task AddAsync(Offer offer, CancellationToken ct = default);
    void Update(Offer offer);
}

public interface IPipelineStageRepository
{
    Task<List<PipelineStage>> GetAllAsync(Guid tenantId, CancellationToken ct = default);
    Task<PipelineStage?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task AddAsync(PipelineStage stage, CancellationToken ct = default);
    void Update(PipelineStage stage);
    void Delete(PipelineStage stage);
}

using WorkBase.Modules.Sales.Application.Contracts;
using WorkBase.Modules.Sales.Application.Dtos;
using WorkBase.Shared.Cqrs;
using WorkBase.Shared.Domain;

namespace WorkBase.Modules.Sales.Application.Queries;

public sealed record GetLeadsQuery : IQuery<List<LeadDto>>, ITenantRequest
{
    public Guid TenantId { get; set; }
}

public sealed class GetLeadsHandler(ILeadRepository repo) : IQueryHandler<GetLeadsQuery, List<LeadDto>>
{
    public async Task<Result<List<LeadDto>>> Handle(GetLeadsQuery q, CancellationToken ct)
    {
        var leads = await repo.GetAllAsync(q.TenantId, ct);
        return leads.Select(l => new LeadDto(l.Id, l.CompanyName, l.ContactName, l.Email, l.Phone,
            l.Source, l.Status, l.AssigneeId, l.EstimatedValue, l.Notes, l.ContactId,
            l.CreatedAt, l.ModifiedAt)).ToList();
    }
}

public sealed record GetLeadByIdQuery(Guid LeadId) : IQuery<LeadDto>, ITenantRequest
{
    public Guid TenantId { get; set; }
}

public sealed class GetLeadByIdHandler(ILeadRepository repo) : IQueryHandler<GetLeadByIdQuery, LeadDto>
{
    public async Task<Result<LeadDto>> Handle(GetLeadByIdQuery q, CancellationToken ct)
    {
        var l = await repo.GetByIdAsync(q.LeadId, ct);
        if (l is null) return Result.Failure<LeadDto>(Error.NotFound("Lead.NotFound", "Lead nie został znaleziony."));
        return new LeadDto(l.Id, l.CompanyName, l.ContactName, l.Email, l.Phone,
            l.Source, l.Status, l.AssigneeId, l.EstimatedValue, l.Notes, l.ContactId,
            l.CreatedAt, l.ModifiedAt);
    }
}

public sealed record GetOpportunitiesQuery : IQuery<List<OpportunityDto>>, ITenantRequest
{
    public Guid TenantId { get; set; }
}

public sealed class GetOpportunitiesHandler(IOpportunityRepository repo) : IQueryHandler<GetOpportunitiesQuery, List<OpportunityDto>>
{
    public async Task<Result<List<OpportunityDto>>> Handle(GetOpportunitiesQuery q, CancellationToken ct)
    {
        var opps = await repo.GetAllAsync(q.TenantId, ct);
        return opps.Select(o => new OpportunityDto(o.Id, o.Name, o.ContactId, o.LeadId, o.AssigneeId,
            o.Stage, o.Value, o.Currency, o.Probability, o.ExpectedCloseDate, o.ClosedAt,
            o.LostReason, o.Notes, o.CreatedAt, o.ModifiedAt)).ToList();
    }
}

public sealed record GetPipelineSummaryQuery : IQuery<List<PipelineSummaryDto>>, ITenantRequest
{
    public Guid TenantId { get; set; }
}

public sealed class GetPipelineSummaryHandler(IOpportunityRepository repo, IPipelineStageRepository stageRepo) : IQueryHandler<GetPipelineSummaryQuery, List<PipelineSummaryDto>>
{
    public async Task<Result<List<PipelineSummaryDto>>> Handle(GetPipelineSummaryQuery q, CancellationToken ct)
    {
        var opps = await repo.GetAllAsync(q.TenantId, ct);
        var stages = await stageRepo.GetAllAsync(q.TenantId, ct);
        var summary = stages.OrderBy(s => s.SortOrder).Select(s =>
        {
            var stageOpps = opps.Where(o => o.Stage.ToString() == s.Name);
            return new PipelineSummaryDto(s.Name, s.Color, stageOpps.Count(), stageOpps.Sum(o => o.Value));
        }).ToList();
        return summary;
    }
}

public sealed record GetOffersQuery(Guid OpportunityId) : IQuery<List<OfferDto>>, ITenantRequest
{
    public Guid TenantId { get; set; }
}

public sealed class GetOffersHandler(IOfferRepository repo) : IQueryHandler<GetOffersQuery, List<OfferDto>>
{
    public async Task<Result<List<OfferDto>>> Handle(GetOffersQuery q, CancellationToken ct)
    {
        var offers = await repo.GetByOpportunityAsync(q.OpportunityId, ct);
        return offers.Select(o => new OfferDto(o.Id, o.OpportunityId, o.Number, o.Title,
            o.TotalNet, o.TotalGross, o.Currency, o.Status, o.ValidUntil, o.SentAt,
            o.Notes, o.ItemsJson, o.CreatedAt, o.ModifiedAt)).ToList();
    }
}

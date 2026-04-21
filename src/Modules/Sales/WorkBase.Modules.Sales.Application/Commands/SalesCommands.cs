using WorkBase.Modules.Sales.Application.Contracts;
using WorkBase.Modules.Sales.Application.Dtos;
using WorkBase.Modules.Sales.Domain.Entities;
using WorkBase.Shared.Cqrs;
using WorkBase.Shared.Domain;

namespace WorkBase.Modules.Sales.Application.Commands;

// ─── Leads ───

public sealed record CreateLeadCommand(
    string CompanyName, LeadSource Source, string? ContactName, string? Email,
    string? Phone, Guid? AssigneeId, decimal? EstimatedValue) : ICommand<Guid>, ITenantRequest
{
    public Guid TenantId { get; set; }
}

public sealed class CreateLeadHandler(ILeadRepository repo) : ICommandHandler<CreateLeadCommand, Guid>
{
    public async Task<Result<Guid>> Handle(CreateLeadCommand cmd, CancellationToken ct)
    {
        var lead = Lead.Create(cmd.TenantId, cmd.CompanyName, cmd.Source, cmd.ContactName,
            cmd.Email, cmd.Phone, cmd.AssigneeId, cmd.EstimatedValue);
        await repo.AddAsync(lead, ct);
        return lead.Id;
    }
}

public sealed record UpdateLeadCommand(
    Guid LeadId, string CompanyName, string? ContactName, string? Email,
    string? Phone, decimal? EstimatedValue, string? Notes) : ICommand, ITenantRequest
{
    public Guid TenantId { get; set; }
}

public sealed class UpdateLeadHandler(ILeadRepository repo) : ICommandHandler<UpdateLeadCommand>
{
    public async Task<Result> Handle(UpdateLeadCommand cmd, CancellationToken ct)
    {
        var lead = await repo.GetByIdAsync(cmd.LeadId, ct);
        if (lead is null) return Result.Failure(Error.NotFound("Lead.NotFound", "Lead nie został znaleziony."));
        lead.Update(cmd.CompanyName, cmd.ContactName, cmd.Email, cmd.Phone, cmd.EstimatedValue, cmd.Notes);
        repo.Update(lead);
        return Result.Success();
    }
}

public sealed record QualifyLeadCommand(Guid LeadId) : ICommand, ITenantRequest
{
    public Guid TenantId { get; set; }
}

public sealed class QualifyLeadHandler(ILeadRepository repo) : ICommandHandler<QualifyLeadCommand>
{
    public async Task<Result> Handle(QualifyLeadCommand cmd, CancellationToken ct)
    {
        var lead = await repo.GetByIdAsync(cmd.LeadId, ct);
        if (lead is null) return Result.Failure(Error.NotFound("Lead.NotFound", "Lead nie został znaleziony."));
        lead.Qualify();
        repo.Update(lead);
        return Result.Success();
    }
}

public sealed record ConvertLeadCommand(Guid LeadId, Guid ContactId) : ICommand<Guid>, ITenantRequest
{
    public Guid TenantId { get; set; }
}

public sealed class ConvertLeadHandler(ILeadRepository leadRepo, IOpportunityRepository oppRepo) : ICommandHandler<ConvertLeadCommand, Guid>
{
    public async Task<Result<Guid>> Handle(ConvertLeadCommand cmd, CancellationToken ct)
    {
        var lead = await leadRepo.GetByIdAsync(cmd.LeadId, ct);
        if (lead is null) return Result.Failure<Guid>(Error.NotFound("Lead.NotFound", "Lead nie został znaleziony."));
        lead.Convert(cmd.ContactId);
        leadRepo.Update(lead);
        var opp = Opportunity.Create(cmd.TenantId, lead.CompanyName, lead.EstimatedValue ?? 0,
            contactId: cmd.ContactId, leadId: lead.Id, assigneeId: lead.AssigneeId);
        await oppRepo.AddAsync(opp, ct);
        return opp.Id;
    }
}

// ─── Opportunities ───

public sealed record CreateOpportunityCommand(
    string Name, decimal Value, string Currency, Guid? ContactId, Guid? AssigneeId,
    int Probability, DateTime? ExpectedCloseDate) : ICommand<Guid>, ITenantRequest
{
    public Guid TenantId { get; set; }
}

public sealed class CreateOpportunityHandler(IOpportunityRepository repo) : ICommandHandler<CreateOpportunityCommand, Guid>
{
    public async Task<Result<Guid>> Handle(CreateOpportunityCommand cmd, CancellationToken ct)
    {
        var opp = Opportunity.Create(cmd.TenantId, cmd.Name, cmd.Value,
            contactId: cmd.ContactId, assigneeId: cmd.AssigneeId,
            probability: cmd.Probability, expectedCloseDate: cmd.ExpectedCloseDate, currency: cmd.Currency);
        await repo.AddAsync(opp, ct);
        return opp.Id;
    }
}

public sealed record AdvanceOpportunityCommand(Guid OpportunityId, DealStage Stage, int Probability) : ICommand, ITenantRequest
{
    public Guid TenantId { get; set; }
}

public sealed class AdvanceOpportunityHandler(IOpportunityRepository repo) : ICommandHandler<AdvanceOpportunityCommand>
{
    public async Task<Result> Handle(AdvanceOpportunityCommand cmd, CancellationToken ct)
    {
        var opp = await repo.GetByIdAsync(cmd.OpportunityId, ct);
        if (opp is null) return Result.Failure(Error.NotFound("Opportunity.NotFound", "Szansa nie została znaleziona."));
        opp.AdvanceStage(cmd.Stage, cmd.Probability);
        repo.Update(opp);
        return Result.Success();
    }
}

public sealed record CloseOpportunityCommand(Guid OpportunityId, bool Won, string? LostReason = null) : ICommand, ITenantRequest
{
    public Guid TenantId { get; set; }
}

public sealed class CloseOpportunityHandler(IOpportunityRepository repo) : ICommandHandler<CloseOpportunityCommand>
{
    public async Task<Result> Handle(CloseOpportunityCommand cmd, CancellationToken ct)
    {
        var opp = await repo.GetByIdAsync(cmd.OpportunityId, ct);
        if (opp is null) return Result.Failure(Error.NotFound("Opportunity.NotFound", "Szansa nie została znaleziona."));
        if (cmd.Won) opp.CloseWon(); else opp.CloseLost(cmd.LostReason ?? "");
        repo.Update(opp);
        return Result.Success();
    }
}

// ─── Offers ───

public sealed record CreateOfferCommand(
    Guid OpportunityId, string Number, string? Title, decimal TotalNet, decimal TotalGross,
    string Currency, DateTime? ValidUntil, string ItemsJson) : ICommand<Guid>, ITenantRequest
{
    public Guid TenantId { get; set; }
}

public sealed class CreateOfferHandler(IOfferRepository repo) : ICommandHandler<CreateOfferCommand, Guid>
{
    public async Task<Result<Guid>> Handle(CreateOfferCommand cmd, CancellationToken ct)
    {
        var offer = Offer.Create(cmd.TenantId, cmd.OpportunityId, cmd.Number, cmd.Title,
            cmd.TotalNet, cmd.TotalGross, cmd.Currency, cmd.ValidUntil, cmd.ItemsJson);
        await repo.AddAsync(offer, ct);
        return offer.Id;
    }
}

public sealed record SendOfferCommand(Guid OfferId) : ICommand, ITenantRequest
{
    public Guid TenantId { get; set; }
}

public sealed class SendOfferHandler(IOfferRepository repo) : ICommandHandler<SendOfferCommand>
{
    public async Task<Result> Handle(SendOfferCommand cmd, CancellationToken ct)
    {
        var offer = await repo.GetByIdAsync(cmd.OfferId, ct);
        if (offer is null) return Result.Failure(Error.NotFound("Offer.NotFound", "Oferta nie została znaleziona."));
        offer.Send();
        repo.Update(offer);
        return Result.Success();
    }
}

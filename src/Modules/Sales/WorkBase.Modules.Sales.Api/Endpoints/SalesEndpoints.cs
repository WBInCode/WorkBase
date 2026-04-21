using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using WorkBase.Modules.Sales.Application.Commands;
using WorkBase.Modules.Sales.Application.Dtos;
using WorkBase.Modules.Sales.Application.Queries;
using WorkBase.Modules.Sales.Domain.Entities;
using WorkBase.Shared.Api;
using WorkBase.Shared.Auth;

namespace WorkBase.Modules.Sales.Api.Endpoints;

public static class SalesEndpoints
{
    public static IEndpointRouteBuilder MapSalesEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var leads = endpoints.MapGroup("/api/sales/leads").WithTags("Leads").RequireAuthorization();
        var opps = endpoints.MapGroup("/api/sales/opportunities").WithTags("Opportunities").RequireAuthorization();
        var offers = endpoints.MapGroup("/api/sales/offers").WithTags("Offers").RequireAuthorization();
        var pipeline = endpoints.MapGroup("/api/sales/pipeline").WithTags("Pipeline").RequireAuthorization();

        // ─── Leads ───
        leads.MapGet("/", async (ISender sender) =>
        {
            var result = await sender.Send(new GetLeadsQuery());
            return result.IsSuccess ? Results.Ok(result.Value) : result.ToHttpResult();
        }).WithName("GetLeads").RequirePermission("sales.view");

        leads.MapGet("/{id:guid}", async (Guid id, ISender sender) =>
        {
            var result = await sender.Send(new GetLeadByIdQuery(id));
            return result.IsSuccess ? Results.Ok(result.Value) : result.ToHttpResult();
        }).WithName("GetLeadById").RequirePermission("sales.view");

        leads.MapPost("/", async (CreateLeadRequest req, ISender sender) =>
        {
            var result = await sender.Send(new CreateLeadCommand(req.CompanyName, req.Source,
                req.ContactName, req.Email, req.Phone, req.AssigneeId, req.EstimatedValue));
            return result.IsSuccess ? Results.Created($"/api/sales/leads/{result.Value}", result.Value) : result.ToHttpResult();
        }).WithName("CreateLead").RequirePermission("sales.manage");

        leads.MapPut("/{id:guid}", async (Guid id, UpdateLeadRequest req, ISender sender) =>
        {
            var result = await sender.Send(new UpdateLeadCommand(id, req.CompanyName, req.ContactName,
                req.Email, req.Phone, req.EstimatedValue, req.Notes));
            return result.IsSuccess ? Results.NoContent() : result.ToHttpResult();
        }).WithName("UpdateLead").RequirePermission("sales.manage");

        leads.MapPost("/{id:guid}/qualify", async (Guid id, ISender sender) =>
        {
            var result = await sender.Send(new QualifyLeadCommand(id));
            return result.IsSuccess ? Results.NoContent() : result.ToHttpResult();
        }).WithName("QualifyLead").RequirePermission("sales.manage");

        leads.MapPost("/{id:guid}/convert", async (Guid id, ConvertLeadRequest req, ISender sender) =>
        {
            var result = await sender.Send(new ConvertLeadCommand(id, req.ContactId));
            return result.IsSuccess ? Results.Ok(new { OpportunityId = result.Value }) : result.ToHttpResult();
        }).WithName("ConvertLead").RequirePermission("sales.manage");

        // ─── Opportunities ───
        opps.MapGet("/", async (ISender sender) =>
        {
            var result = await sender.Send(new GetOpportunitiesQuery());
            return result.IsSuccess ? Results.Ok(result.Value) : result.ToHttpResult();
        }).WithName("GetOpportunities").RequirePermission("sales.view");

        opps.MapPost("/", async (CreateOpportunityRequest req, ISender sender) =>
        {
            var result = await sender.Send(new CreateOpportunityCommand(req.Name, req.Value, req.Currency,
                req.ContactId, req.AssigneeId, req.Probability, req.ExpectedCloseDate));
            return result.IsSuccess ? Results.Created($"/api/sales/opportunities/{result.Value}", result.Value) : result.ToHttpResult();
        }).WithName("CreateOpportunity").RequirePermission("sales.manage");

        opps.MapPost("/{id:guid}/advance", async (Guid id, AdvanceStageRequest req, ISender sender) =>
        {
            var result = await sender.Send(new AdvanceOpportunityCommand(id, req.Stage, req.Probability));
            return result.IsSuccess ? Results.NoContent() : result.ToHttpResult();
        }).WithName("AdvanceOpportunity").RequirePermission("sales.manage");

        opps.MapPost("/{id:guid}/close", async (Guid id, CloseOpportunityRequest req, ISender sender) =>
        {
            var result = await sender.Send(new CloseOpportunityCommand(id, req.Won, req.LostReason));
            return result.IsSuccess ? Results.NoContent() : result.ToHttpResult();
        }).WithName("CloseOpportunity").RequirePermission("sales.manage");

        // ─── Offers ───
        offers.MapGet("/opportunity/{opportunityId:guid}", async (Guid opportunityId, ISender sender) =>
        {
            var result = await sender.Send(new GetOffersQuery(opportunityId));
            return result.IsSuccess ? Results.Ok(result.Value) : result.ToHttpResult();
        }).WithName("GetOffers").RequirePermission("sales.view");

        offers.MapPost("/", async (CreateOfferRequest req, ISender sender) =>
        {
            var result = await sender.Send(new CreateOfferCommand(req.OpportunityId, req.Number, req.Title,
                req.TotalNet, req.TotalGross, req.Currency, req.ValidUntil, req.ItemsJson));
            return result.IsSuccess ? Results.Created($"/api/sales/offers/{result.Value}", result.Value) : result.ToHttpResult();
        }).WithName("CreateOffer").RequirePermission("sales.manage");

        offers.MapPost("/{id:guid}/send", async (Guid id, ISender sender) =>
        {
            var result = await sender.Send(new SendOfferCommand(id));
            return result.IsSuccess ? Results.NoContent() : result.ToHttpResult();
        }).WithName("SendOffer").RequirePermission("sales.manage");

        // ─── Pipeline ───
        pipeline.MapGet("/summary", async (ISender sender) =>
        {
            var result = await sender.Send(new GetPipelineSummaryQuery());
            return result.IsSuccess ? Results.Ok(result.Value) : result.ToHttpResult();
        }).WithName("GetPipelineSummary").RequirePermission("sales.view");

        return endpoints;
    }
}

// Request DTOs
public sealed record CreateLeadRequest(string CompanyName, LeadSource Source, string? ContactName, string? Email, string? Phone, Guid? AssigneeId, decimal? EstimatedValue);
public sealed record UpdateLeadRequest(string CompanyName, string? ContactName, string? Email, string? Phone, decimal? EstimatedValue, string? Notes);
public sealed record ConvertLeadRequest(Guid ContactId);
public sealed record CreateOpportunityRequest(string Name, decimal Value, string Currency, Guid? ContactId, Guid? AssigneeId, int Probability, DateTime? ExpectedCloseDate);
public sealed record AdvanceStageRequest(DealStage Stage, int Probability);
public sealed record CloseOpportunityRequest(bool Won, string? LostReason);
public sealed record CreateOfferRequest(Guid OpportunityId, string Number, string? Title, decimal TotalNet, decimal TotalGross, string Currency, DateTime? ValidUntil, string ItemsJson);

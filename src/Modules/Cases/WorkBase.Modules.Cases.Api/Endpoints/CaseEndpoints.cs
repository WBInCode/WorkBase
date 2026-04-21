using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using WorkBase.Modules.Cases.Application.Commands;
using WorkBase.Modules.Cases.Application.Dtos;
using WorkBase.Modules.Cases.Application.Queries;
using WorkBase.Modules.Cases.Domain.Entities;
using WorkBase.Shared.Api;
using WorkBase.Shared.Auth;

namespace WorkBase.Modules.Cases.Api.Endpoints;

public static class CaseEndpoints
{
    public static IEndpointRouteBuilder MapCaseEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/cases")
            .WithTags("Cases")
            .RequireAuthorization();

        group.MapGet("/", GetCases)
            .WithName("GetCases").WithSummary("Lista spraw")
            .RequirePermission("cases.view").Produces<List<CaseItemDto>>();

        group.MapGet("/{id:guid}", GetCaseById)
            .WithName("GetCaseById").WithSummary("Szczegóły sprawy")
            .RequirePermission("cases.view").Produces<CaseItemDto>();

        group.MapPost("/", CreateCase)
            .WithName("CreateCase").WithSummary("Utwórz sprawę")
            .RequirePermission("cases.create").Produces<Guid>(StatusCodes.Status201Created);

        group.MapPut("/{id:guid}", UpdateCase)
            .WithName("UpdateCase").WithSummary("Aktualizuj sprawę")
            .RequirePermission("cases.edit").Produces(StatusCodes.Status204NoContent);

        group.MapPut("/{id:guid}/status", ChangeCaseStatus)
            .WithName("ChangeCaseStatus").WithSummary("Zmień status sprawy")
            .RequirePermission("cases.edit").Produces(StatusCodes.Status204NoContent);

        group.MapPut("/{id:guid}/assign", AssignCase)
            .WithName("AssignCase").WithSummary("Przypisz sprawę")
            .RequirePermission("cases.assign").Produces(StatusCodes.Status204NoContent);

        group.MapGet("/{id:guid}/comments", GetCaseComments)
            .WithName("GetCaseComments").WithSummary("Komentarze sprawy")
            .RequirePermission("cases.view").Produces<List<CaseCommentDto>>();

        group.MapPost("/{id:guid}/comments", AddCaseComment)
            .WithName("AddCaseComment").WithSummary("Dodaj komentarz")
            .RequirePermission("cases.comment").Produces<Guid>(StatusCodes.Status201Created);

        group.MapGet("/statuses", GetCaseStatuses)
            .WithName("GetCaseStatuses").WithSummary("Statusy spraw")
            .RequirePermission("cases.view").Produces<List<CaseStatusDto>>();

        group.MapGet("/categories", GetCaseCategories)
            .WithName("GetCaseCategories").WithSummary("Kategorie spraw")
            .RequirePermission("cases.view").Produces<List<CaseCategoryDto>>();

        return endpoints;
    }

    private static async Task<IResult> GetCases(ISender sender, Guid? assigneeId = null, Guid? contactId = null)
    {
        var result = await sender.Send(new GetCasesQuery(assigneeId, contactId));
        return result.IsSuccess ? Results.Ok(result.Value) : result.ToHttpResult();
    }

    private static async Task<IResult> GetCaseById(Guid id, ISender sender)
    {
        var result = await sender.Send(new GetCaseByIdQuery(id));
        return result.IsSuccess ? Results.Ok(result.Value) : result.ToHttpResult();
    }

    private static async Task<IResult> CreateCase(CreateCaseBody body, ISender sender)
    {
        var result = await sender.Send(new CreateCaseCommand(
            body.Title, body.Description, body.Priority,
            body.CategoryId, body.AssigneeId, body.ContactId, body.DueDate));
        return result.IsSuccess
            ? Results.Created($"/api/cases/{result.Value}", result.Value)
            : result.ToHttpResult();
    }

    private static async Task<IResult> UpdateCase(Guid id, UpdateCaseBody body, ISender sender)
    {
        var result = await sender.Send(new UpdateCaseCommand(id, body.Title, body.Description, body.Priority, body.DueDate, body.CategoryId));
        return result.IsSuccess ? Results.NoContent() : result.ToHttpResult();
    }

    private static async Task<IResult> ChangeCaseStatus(Guid id, ChangeCaseStatusBody body, ISender sender, HttpContext ctx)
    {
        var userId = Guid.Parse(ctx.User.FindFirst("sub")?.Value ?? Guid.Empty.ToString());
        var result = await sender.Send(new ChangeCaseStatusCommand(id, body.StatusId, userId));
        return result.IsSuccess ? Results.NoContent() : result.ToHttpResult();
    }

    private static async Task<IResult> AssignCase(Guid id, AssignCaseBody body, ISender sender)
    {
        var result = await sender.Send(new AssignCaseCommand(id, body.AssigneeId));
        return result.IsSuccess ? Results.NoContent() : result.ToHttpResult();
    }

    private static async Task<IResult> GetCaseComments(Guid id, ISender sender)
    {
        var result = await sender.Send(new GetCaseCommentsQuery(id));
        return result.IsSuccess ? Results.Ok(result.Value) : result.ToHttpResult();
    }

    private static async Task<IResult> AddCaseComment(Guid id, AddCaseCommentBody body, ISender sender, HttpContext ctx)
    {
        var userId = Guid.Parse(ctx.User.FindFirst("sub")?.Value ?? Guid.Empty.ToString());
        var result = await sender.Send(new AddCaseCommentCommand(id, body.Content, body.IsInternal, userId));
        return result.IsSuccess
            ? Results.Created($"/api/cases/{id}/comments/{result.Value}", result.Value)
            : result.ToHttpResult();
    }

    private static async Task<IResult> GetCaseStatuses(ISender sender)
    {
        var result = await sender.Send(new GetCaseStatusesQuery());
        return result.IsSuccess ? Results.Ok(result.Value) : result.ToHttpResult();
    }

    private static async Task<IResult> GetCaseCategories(ISender sender)
    {
        var result = await sender.Send(new GetCaseCategoriesQuery());
        return result.IsSuccess ? Results.Ok(result.Value) : result.ToHttpResult();
    }
}

public sealed record CreateCaseBody(string Title, string? Description, CasePriorityLevel Priority, Guid? CategoryId, Guid? AssigneeId, Guid? ContactId, DateTime? DueDate);
public sealed record UpdateCaseBody(string Title, string? Description, CasePriorityLevel Priority, DateTime? DueDate, Guid? CategoryId);
public sealed record ChangeCaseStatusBody(Guid StatusId);
public sealed record AssignCaseBody(Guid? AssigneeId);
public sealed record AddCaseCommentBody(string Content, bool IsInternal);

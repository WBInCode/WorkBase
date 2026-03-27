using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using WorkBase.Modules.Workflow.Application.Commands;
using WorkBase.Modules.Workflow.Application.Dtos;
using WorkBase.Modules.Workflow.Application.Queries;
using WorkBase.Shared.Api;
using WorkBase.Shared.Auth;

namespace WorkBase.Modules.Workflow.Api.Endpoints;

public static class WorkflowEndpoints
{
    public static IEndpointRouteBuilder MapWorkflowEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/workflow")
            .WithTags("Workflow")
            .RequireAuthorization();

        // --- Definitions ---
        group.MapGet("/definitions", GetDefinitions)
            .WithName("GetWorkflowDefinitions")
            .WithSummary("Pobierz listę definicji workflow")
            .RequirePermission("workflow.view")
            .Produces<List<WorkflowDefinitionDto>>();

        group.MapPost("/definitions", CreateDefinition)
            .WithName("CreateWorkflowDefinition")
            .WithSummary("Utwórz nową definicję workflow")
            .RequirePermission("workflow.manage")
            .Produces<Guid>(StatusCodes.Status201Created);

        group.MapPut("/definitions/{id:guid}", UpdateDefinition)
            .WithName("UpdateWorkflowDefinition")
            .WithSummary("Aktualizuj definicję workflow")
            .RequirePermission("workflow.manage")
            .Produces(StatusCodes.Status204NoContent);

        // --- Instances ---
        group.MapPost("/instances", CreateInstance)
            .WithName("CreateWorkflowInstance")
            .WithSummary("Utwórz nową instancję workflow")
            .RequirePermission("workflow.create")
            .Produces<Guid>(StatusCodes.Status201Created);

        group.MapGet("/instances/{id:guid}", GetInstance)
            .WithName("GetWorkflowInstance")
            .WithSummary("Pobierz instancję workflow")
            .RequirePermission("workflow.view")
            .Produces<WorkflowInstanceDto>();

        group.MapGet("/instances/{id:guid}/steps", GetSteps)
            .WithName("GetWorkflowSteps")
            .WithSummary("Pobierz historię kroków instancji workflow")
            .RequirePermission("workflow.view")
            .Produces<List<WorkflowStepDto>>();

        group.MapPost("/instances/{id:guid}/advance", AdvanceWorkflow)
            .WithName("AdvanceWorkflow")
            .WithSummary("Przesuń workflow do następnego kroku")
            .RequirePermission("workflow.create")
            .Produces<string>();

        group.MapPost("/instances/{id:guid}/cancel", CancelWorkflow)
            .WithName("CancelWorkflow")
            .WithSummary("Anuluj instancję workflow")
            .RequirePermission("workflow.manage")
            .Produces(StatusCodes.Status204NoContent);

        return endpoints;
    }

    private static async Task<IResult> GetDefinitions(ISender sender)
    {
        var query = new GetWorkflowDefinitionsQuery();
        var result = await sender.Send(query);
        return result.ToHttpResult();
    }

    private static async Task<IResult> CreateDefinition(
        CreateWorkflowDefinitionRequest request,
        ISender sender)
    {
        var command = new CreateWorkflowDefinitionCommand(
            request.Name,
            request.DefinitionJson,
            request.Description);

        var result = await sender.Send(command);

        return result.IsSuccess
            ? Results.Created($"/api/workflow/definitions/{result.Value}", result.Value)
            : result.ToHttpResult();
    }

    private static async Task<IResult> UpdateDefinition(
        Guid id,
        UpdateWorkflowDefinitionRequest request,
        ISender sender)
    {
        var command = new UpdateWorkflowDefinitionCommand(
            id,
            request.Name,
            request.DefinitionJson,
            request.Description);

        var result = await sender.Send(command);
        return result.ToHttpResult();
    }

    private static async Task<IResult> CreateInstance(
        CreateWorkflowInstanceRequest request,
        ISender sender)
    {
        var command = new CreateWorkflowInstanceCommand(
            request.DefinitionId,
            request.EntityType,
            request.EntityId,
            request.InitiatedBy);

        var result = await sender.Send(command);

        return result.IsSuccess
            ? Results.Created($"/api/workflow/instances/{result.Value}", result.Value)
            : result.ToHttpResult();
    }

    private static async Task<IResult> GetInstance(Guid id, ISender sender)
    {
        var query = new GetWorkflowInstanceQuery(id);
        var result = await sender.Send(query);
        return result.ToHttpResult();
    }

    private static async Task<IResult> GetSteps(Guid id, ISender sender)
    {
        var query = new GetWorkflowStepsQuery(id);
        var result = await sender.Send(query);
        return result.ToHttpResult();
    }

    private static async Task<IResult> AdvanceWorkflow(
        Guid id,
        AdvanceWorkflowRequest request,
        ISender sender)
    {
        var command = new AdvanceWorkflowCommand(
            id,
            request.Outcome,
            request.CompletedBy,
            request.Comment);

        var result = await sender.Send(command);
        return result.ToHttpResult();
    }

    private static async Task<IResult> CancelWorkflow(Guid id, ISender sender)
    {
        var command = new CancelWorkflowCommand(id);
        var result = await sender.Send(command);
        return result.ToHttpResult();
    }
}

public sealed record CreateWorkflowDefinitionRequest(string Name, string DefinitionJson, string? Description = null);
public sealed record UpdateWorkflowDefinitionRequest(string Name, string DefinitionJson, string? Description = null);
public sealed record CreateWorkflowInstanceRequest(Guid DefinitionId, string EntityType, Guid EntityId, Guid InitiatedBy);
public sealed record AdvanceWorkflowRequest(string Outcome, string? CompletedBy = null, string? Comment = null);

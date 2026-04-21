using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using WorkBase.Modules.Forms.Application.Commands;
using WorkBase.Modules.Forms.Application.Dtos;
using WorkBase.Modules.Forms.Application.Queries;
using WorkBase.Shared.Api;
using WorkBase.Shared.Auth;

namespace WorkBase.Modules.Forms.Api.Endpoints;

public static class FormsEndpoints
{
    public static IEndpointRouteBuilder MapFormsEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/forms")
            .WithTags("Forms")
            .RequireAuthorization();

        group.MapGet("/definitions", async (ISender sender) =>
        {
            var result = await sender.Send(new GetFormDefinitionsQuery());
            return result.ToHttpResult();
        })
        .WithName("GetFormDefinitions")
        .WithSummary("Pobierz listę definicji formularzy")
        .RequirePermission("forms.view")
        .Produces<List<FormDefinitionDto>>();

        group.MapGet("/definitions/{id:guid}", async (Guid id, ISender sender) =>
        {
            var result = await sender.Send(new GetFormDefinitionByIdQuery(id));
            return result.ToHttpResult();
        })
        .WithName("GetFormDefinitionById")
        .WithSummary("Pobierz definicję formularza po ID")
        .RequirePermission("forms.view")
        .Produces<FormDefinitionDto>()
        .Produces(StatusCodes.Status404NotFound);

        group.MapPost("/definitions", async (CreateFormDefinitionRequest body, ISender sender) =>
        {
            var cmd = new CreateFormDefinitionCommand(
                body.Name, body.Description, body.IsPublic,
                body.WorkflowDefinitionName, body.Fields);
            var result = await sender.Send(cmd);
            return result.IsSuccess
                ? Results.Created($"/api/forms/definitions/{result.Value}", result.Value)
                : result.ToHttpResult();
        })
        .WithName("CreateFormDefinition")
        .WithSummary("Utwórz nową definicję formularza")
        .RequirePermission("forms.manage")
        .Produces<Guid>(StatusCodes.Status201Created);

        group.MapPut("/definitions/{id:guid}", async (Guid id, UpdateFormDefinitionRequest body, ISender sender) =>
        {
            var cmd = new UpdateFormDefinitionCommand(
                id, body.Name, body.Description, body.IsPublic,
                body.WorkflowDefinitionName, body.Fields);
            var result = await sender.Send(cmd);
            return result.IsSuccess ? Results.NoContent() : result.ToHttpResult();
        })
        .WithName("UpdateFormDefinition")
        .WithSummary("Aktualizuj definicję formularza")
        .RequirePermission("forms.manage")
        .Produces(StatusCodes.Status204NoContent);

        group.MapDelete("/definitions/{id:guid}", async (Guid id, ISender sender) =>
        {
            var result = await sender.Send(new DeleteFormDefinitionCommand(id));
            return result.IsSuccess ? Results.NoContent() : result.ToHttpResult();
        })
        .WithName("DeleteFormDefinition")
        .WithSummary("Usuń definicję formularza")
        .RequirePermission("forms.manage")
        .Produces(StatusCodes.Status204NoContent);

        group.MapPost("/submit", async (SubmitFormRequest body, ISender sender) =>
        {
            var cmd = new SubmitFormCommand(body.FormDefinitionId, body.ValuesJson);
            var result = await sender.Send(cmd);
            return result.IsSuccess
                ? Results.Created($"/api/forms/submissions/{result.Value}", result.Value)
                : result.ToHttpResult();
        })
        .WithName("SubmitForm")
        .WithSummary("Wyślij wypełniony formularz")
        .RequirePermission("forms.submit")
        .Produces<Guid>(StatusCodes.Status201Created);

        group.MapGet("/submissions/{formDefinitionId:guid}", async (Guid formDefinitionId, ISender sender) =>
        {
            var result = await sender.Send(new GetFormSubmissionsQuery(formDefinitionId));
            return result.ToHttpResult();
        })
        .WithName("GetFormSubmissions")
        .WithSummary("Pobierz zgłoszenia formularza")
        .RequirePermission("forms.view")
        .Produces<List<FormSubmissionDto>>();

        return endpoints;
    }
}

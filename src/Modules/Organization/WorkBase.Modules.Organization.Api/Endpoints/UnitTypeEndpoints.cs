using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using WorkBase.Modules.Organization.Application.Commands.UnitTypes;
using WorkBase.Modules.Organization.Application.Dtos;
using WorkBase.Modules.Organization.Application.Queries.UnitTypes;
using WorkBase.Shared.Auth;

namespace WorkBase.Modules.Organization.Api.Endpoints;

public static class UnitTypeEndpoints
{
    public static IEndpointRouteBuilder MapUnitTypeEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/org/unit-types")
            .WithTags("Unit Types")
            .RequireAuthorization();

        group.MapGet("/", GetUnitTypes)
            .WithName("GetUnitTypes")
            .WithSummary("Pobierz listę typów jednostek (słownik per tenant)")
            .RequirePermission("org.view")
            .Produces<List<OrganizationUnitTypeDto>>();

        group.MapPost("/", CreateUnitType)
            .WithName("CreateUnitType")
            .WithSummary("Utwórz typ jednostki organizacyjnej")
            .RequirePermission("org.create")
            .Produces<Guid>(StatusCodes.Status201Created)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status409Conflict);

        group.MapPut("/{id:guid}", UpdateUnitType)
            .WithName("UpdateUnitType")
            .WithSummary("Zaktualizuj typ jednostki organizacyjnej")
            .RequirePermission("org.edit")
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status404NotFound);

        return endpoints;
    }

    private static async Task<IResult> GetUnitTypes(ISender sender)
    {
        var result = await sender.Send(new GetUnitTypesQuery());
        return result.ToHttpResult();
    }

    private static async Task<IResult> CreateUnitType(
        CreateUnitTypeCommand command,
        ISender sender)
    {
        var result = await sender.Send(command);

        return result.IsSuccess
            ? Results.CreatedAtRoute("GetUnitTypes", null, result.Value)
            : result.ToHttpResult();
    }

    private static async Task<IResult> UpdateUnitType(
        Guid id,
        UpdateUnitTypeRequest request,
        ISender sender)
    {
        var command = new UpdateUnitTypeCommand(id, request.Name, request.Description, request.SortOrder);
        var result = await sender.Send(command);
        return result.ToHttpResult();
    }
}

public sealed record UpdateUnitTypeRequest(
    string Name,
    string? Description,
    int SortOrder);

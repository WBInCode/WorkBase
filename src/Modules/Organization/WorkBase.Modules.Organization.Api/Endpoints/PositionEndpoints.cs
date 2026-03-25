using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using WorkBase.Modules.Organization.Application.Commands.Positions;
using WorkBase.Modules.Organization.Application.Dtos;
using WorkBase.Modules.Organization.Application.Queries.Positions;

namespace WorkBase.Modules.Organization.Api.Endpoints;

public static class PositionEndpoints
{
    public static IEndpointRouteBuilder MapPositionEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/org/positions")
            .WithTags("Positions")
            .RequireAuthorization();

        group.MapGet("/", GetPositions)
            .WithName("GetPositions")
            .WithSummary("Pobierz listę stanowisk (słownik per tenant)")
            .Produces<List<PositionDto>>();

        group.MapPost("/", CreatePosition)
            .WithName("CreatePosition")
            .WithSummary("Utwórz stanowisko")
            .Produces<Guid>(StatusCodes.Status201Created)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status409Conflict);

        group.MapPut("/{id:guid}", UpdatePosition)
            .WithName("UpdatePosition")
            .WithSummary("Zaktualizuj stanowisko")
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status404NotFound);

        return endpoints;
    }

    private static async Task<IResult> GetPositions(ISender sender)
    {
        var result = await sender.Send(new GetPositionsQuery());
        return result.ToHttpResult();
    }

    private static async Task<IResult> CreatePosition(
        CreatePositionCommand command,
        ISender sender)
    {
        var result = await sender.Send(command);

        return result.IsSuccess
            ? Results.CreatedAtRoute("GetPositions", null, result.Value)
            : result.ToHttpResult();
    }

    private static async Task<IResult> UpdatePosition(
        Guid id,
        UpdatePositionRequest request,
        ISender sender)
    {
        var command = new UpdatePositionCommand(id, request.Name, request.Description);
        var result = await sender.Send(command);
        return result.ToHttpResult();
    }
}

public sealed record UpdatePositionRequest(
    string Name,
    string? Description);

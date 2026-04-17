using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using WorkBase.Modules.Organization.Application.Commands.Units;
using WorkBase.Modules.Organization.Application.Dtos;
using WorkBase.Modules.Organization.Application.Queries.Units;
using WorkBase.Shared.Api;
using WorkBase.Shared.Auth;

namespace WorkBase.Modules.Organization.Api.Endpoints;

public static class OrganizationUnitEndpoints
{
    public static IEndpointRouteBuilder MapOrganizationUnitEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/org/units")
            .WithTags("Organization Units")
            .RequireAuthorization();

        group.MapPost("/", CreateUnit)
            .WithName("CreateOrganizationUnit")
            .WithSummary("Utwórz jednostkę organizacyjną")
            .RequirePermission("org.create")
            .Produces<Guid>(StatusCodes.Status201Created)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status409Conflict);

        group.MapGet("/tree", GetUnitTree)
            .WithName("GetUnitTree")
            .WithSummary("Pobierz drzewo hierarchii organizacyjnej")
            .RequirePermission("org.view")
            .Produces<List<OrganizationUnitTreeNodeDto>>();

        group.MapGet("/{id:guid}", GetUnitById)
            .WithName("GetOrganizationUnitById")
            .WithSummary("Pobierz szczegóły jednostki organizacyjnej")
            .RequirePermission("org.view")
            .Produces<OrganizationUnitDto>()
            .Produces(StatusCodes.Status404NotFound);

        group.MapPut("/{id:guid}", UpdateUnit)
            .WithName("UpdateOrganizationUnit")
            .WithSummary("Zaktualizuj jednostkę organizacyjną")
            .RequirePermission("org.edit")
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status404NotFound);

        group.MapDelete("/{id:guid}", DeleteUnit)
            .WithName("DeleteOrganizationUnit")
            .WithSummary("Usuń jednostkę organizacyjną")
            .RequirePermission("org.edit")
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status404NotFound);

        return endpoints;
    }

    private static async Task<IResult> CreateUnit(
        CreateOrganizationUnitCommand command,
        ISender sender)
    {
        var result = await sender.Send(command);

        return result.IsSuccess
            ? Results.CreatedAtRoute("GetUnitTree", null, new { id = result.Value })
            : result.ToHttpResult();
    }

    private static async Task<IResult> GetUnitTree(
        ISender sender)
    {
        var result = await sender.Send(new GetUnitTreeQuery());
        return result.ToHttpResult();
    }

    private static async Task<IResult> GetUnitById(Guid id, ISender sender)
    {
        var result = await sender.Send(new GetUnitByIdQuery(id));
        return result.ToHttpResult();
    }

    private static async Task<IResult> UpdateUnit(
        Guid id,
        UpdateOrganizationUnitRequest request,
        ISender sender)
    {
        var command = new UpdateOrganizationUnitCommand(id, request.Name, request.Code, request.TypeId);
        var result = await sender.Send(command);
        return result.ToHttpResult();
    }

    private static async Task<IResult> DeleteUnit(Guid id, ISender sender)
    {
        var result = await sender.Send(new DeleteOrganizationUnitCommand(id));
        return result.IsSuccess ? Results.NoContent() : result.ToHttpResult();
    }
}

public sealed record UpdateOrganizationUnitRequest(
    string Name,
    string? Code,
    Guid TypeId);

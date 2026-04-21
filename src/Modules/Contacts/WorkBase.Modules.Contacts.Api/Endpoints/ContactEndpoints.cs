using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using WorkBase.Modules.Contacts.Application.Commands;
using WorkBase.Modules.Contacts.Application.Dtos;
using WorkBase.Modules.Contacts.Application.Queries;
using WorkBase.Modules.Contacts.Domain.Entities;
using WorkBase.Shared.Api;
using WorkBase.Shared.Auth;

namespace WorkBase.Modules.Contacts.Api.Endpoints;

public static class ContactEndpoints
{
    public static IEndpointRouteBuilder MapContactEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/contacts")
            .WithTags("Contacts")
            .RequireAuthorization();

        group.MapGet("/", GetContacts)
            .WithName("GetContacts").WithSummary("Lista kontrahentów")
            .RequirePermission("contacts.view").Produces<List<ContactDto>>();

        group.MapGet("/{id:guid}", GetContactById)
            .WithName("GetContactById").WithSummary("Szczegóły kontrahenta")
            .RequirePermission("contacts.view").Produces<ContactDto>();

        group.MapPost("/", CreateContact)
            .WithName("CreateContact").WithSummary("Dodaj kontrahenta")
            .RequirePermission("contacts.create").Produces<Guid>(StatusCodes.Status201Created);

        group.MapPut("/{id:guid}", UpdateContact)
            .WithName("UpdateContact").WithSummary("Edytuj kontrahenta")
            .RequirePermission("contacts.edit").Produces(StatusCodes.Status204NoContent);

        group.MapPut("/{id:guid}/owner", AssignOwner)
            .WithName("AssignContactOwner").WithSummary("Przypisz opiekuna")
            .RequirePermission("contacts.assign").Produces(StatusCodes.Status204NoContent);

        group.MapGet("/{id:guid}/persons", GetContactPersons)
            .WithName("GetContactPersons").WithSummary("Osoby kontaktowe")
            .RequirePermission("contacts.view").Produces<List<ContactPersonDto>>();

        group.MapPost("/{id:guid}/persons", AddContactPerson)
            .WithName("AddContactPerson").WithSummary("Dodaj osobę kontaktową")
            .RequirePermission("contacts.edit").Produces<Guid>(StatusCodes.Status201Created);

        return endpoints;
    }

    private static async Task<IResult> GetContacts(ISender sender, Guid? ownerId = null, string? search = null)
    {
        var result = await sender.Send(new GetContactsQuery(ownerId, search));
        return result.IsSuccess ? Results.Ok(result.Value) : result.ToHttpResult();
    }

    private static async Task<IResult> GetContactById(Guid id, ISender sender)
    {
        var result = await sender.Send(new GetContactByIdQuery(id));
        return result.IsSuccess ? Results.Ok(result.Value) : result.ToHttpResult();
    }

    private static async Task<IResult> CreateContact(CreateContactBody body, ISender sender)
    {
        var result = await sender.Send(new CreateContactCommand(
            body.Name, body.Type, body.Nip, body.Regon,
            body.Email, body.Phone, body.Website,
            body.Street, body.City, body.PostalCode, body.Country,
            body.Notes, body.OwnerId));
        return result.IsSuccess
            ? Results.Created($"/api/contacts/{result.Value}", result.Value)
            : result.ToHttpResult();
    }

    private static async Task<IResult> UpdateContact(Guid id, UpdateContactBody body, ISender sender)
    {
        var result = await sender.Send(new UpdateContactCommand(
            id, body.Name, body.Type, body.Nip, body.Regon,
            body.Email, body.Phone, body.Website,
            body.Street, body.City, body.PostalCode, body.Country, body.Notes));
        return result.IsSuccess ? Results.NoContent() : result.ToHttpResult();
    }

    private static async Task<IResult> AssignOwner(Guid id, AssignOwnerBody body, ISender sender)
    {
        var result = await sender.Send(new AssignContactOwnerCommand(id, body.OwnerId));
        return result.IsSuccess ? Results.NoContent() : result.ToHttpResult();
    }

    private static async Task<IResult> GetContactPersons(Guid id, ISender sender)
    {
        var result = await sender.Send(new GetContactPersonsQuery(id));
        return result.IsSuccess ? Results.Ok(result.Value) : result.ToHttpResult();
    }

    private static async Task<IResult> AddContactPerson(Guid id, AddContactPersonBody body, ISender sender)
    {
        var result = await sender.Send(new AddContactPersonCommand(
            id, body.FirstName, body.LastName, body.Position, body.Email, body.Phone, body.IsPrimary));
        return result.IsSuccess
            ? Results.Created($"/api/contacts/{id}/persons/{result.Value}", result.Value)
            : result.ToHttpResult();
    }
}

public sealed record CreateContactBody(string Name, ContactType Type, string? Nip, string? Regon, string? Email, string? Phone, string? Website, string? Street, string? City, string? PostalCode, string? Country, string? Notes, Guid? OwnerId);
public sealed record UpdateContactBody(string Name, ContactType Type, string? Nip, string? Regon, string? Email, string? Phone, string? Website, string? Street, string? City, string? PostalCode, string? Country, string? Notes);
public sealed record AssignOwnerBody(Guid? OwnerId);
public sealed record AddContactPersonBody(string FirstName, string LastName, string? Position, string? Email, string? Phone, bool IsPrimary);

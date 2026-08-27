using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using WorkBase.Modules.Organization.Application.Commands.ListyKontrolne;
using WorkBase.Shared.Api;
using WorkBase.Shared.Auth;

namespace WorkBase.Modules.Organization.Api.Endpoints;

/// <summary>
/// Listy kontrolne przyjęcia i pożegnania — szablony, które przy zdarzeniu same zakładają zadania.
/// </summary>
/// <remarks>
/// Słownik firmy jak stanowiska czy rodzaje terminów, więc ten sam próg: <c>org.manage</c>.
/// Bez trasy z identyfikatorem w adresie — lista jest krótka i front dostaje ją w całości.
/// </remarks>
public static class ListyKontrolneEndpoints
{
    public static IEndpointRouteBuilder MapListyKontrolneEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/listy-kontrolne")
            .WithTags("ListyKontrolne")
            .RequireAuthorization();

        group.MapGet("/", async (ISender sender, CancellationToken ct) =>
        {
            var wynik = await sender.Send(new PobierzListyKontrolneQuery(), ct);
            return wynik.ToHttpResult();
        })
        .WithName("PobierzListyKontrolne")
        .WithSummary("Listy kontrolne firmy, także wyłączone")
        .RequirePermission("org.manage");

        group.MapPost("/", async (ZapiszListeKontrolnaCommand body, ISender sender, CancellationToken ct) =>
        {
            var wynik = await sender.Send(body, ct);
            return wynik.IsSuccess ? Results.Ok(new { id = wynik.Value }) : wynik.ToHttpResult();
        })
        .WithName("ZapiszListeKontrolna")
        .WithSummary("Dodaje albo zmienia listę razem z pozycjami")
        .RequirePermission("org.manage");

        return endpoints;
    }
}

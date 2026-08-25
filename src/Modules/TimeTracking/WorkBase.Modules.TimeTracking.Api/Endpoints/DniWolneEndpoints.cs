using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using WorkBase.Modules.TimeTracking.Application.Commands;
using WorkBase.Modules.TimeTracking.Domain.Entities;
using WorkBase.Modules.TimeTracking.Domain.Services;
using WorkBase.Shared.Api;
using WorkBase.Shared.Auth;

namespace WorkBase.Modules.TimeTracking.Api.Endpoints;

/// <summary>
/// Kalendarz dni wolnych firmy.
/// </summary>
/// <remarks>
/// Dzien wolny wplywa na dwie rzeczy: obniza norme czasu pracy i pozwala naliczyc dodatek
/// swiateczny. System nie zna z gory zadnych dat — wpisuje je firma. Typowe polskie dni wolne
/// mozna wstawic jednym zadaniem, ale jest to swiadoma decyzja administratora, a nie
/// zachowanie domyslne przy zakladaniu firmy.
/// </remarks>
public static class DniWolneEndpoints
{
    public static IEndpointRouteBuilder MapDniWolneEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/time/dni-wolne")
            .WithTags("DniWolne")
            .RequireAuthorization();

        group.MapGet("/", async (int? rok, ISender sender, CancellationToken ct) =>
        {
            var wynik = await sender.Send(new PobierzDniWolneQuery(rok ?? DateTime.UtcNow.Year), ct);
            return wynik.ToHttpResult();
        })
        .WithName("PobierzDniWolne")
        .WithSummary("Dni wolne firmy w danym roku")
        .RequirePermission("time.view")
        .Produces<List<DzienWolnyDto>>();

        group.MapGet("/propozycje", (int? rok) =>
        {
            var wybranyRok = rok ?? DateTime.UtcNow.Year;
            return Results.Ok(KalendarzPolski.ProponowaneDniWolne(wybranyRok)
                .Select(p => new { data = p.Data, nazwa = p.Nazwa })
                .ToList());
        })
        .WithName("PobierzPropozycjeDniWolnych")
        .WithSummary("Podpowiedź typowych dni wolnych w Polsce — do wglądu przed wstawieniem")
        .RequirePermission("config.manage");

        group.MapPost("/", async (
            ZapiszDzienWolnyRequest request, ISender sender, CancellationToken ct) =>
        {
            var wynik = await sender.Send(new DodajDzienWolnyCommand(
                request.Data, request.Nazwa, request.Rodzaj, request.ObnizaNorme), ct);

            return wynik.IsSuccess
                ? Results.Created($"/api/time/dni-wolne/{wynik.Value}", wynik.Value)
                : wynik.ToHttpResult();
        })
        .WithName("DodajDzienWolny")
        .WithSummary("Dodaje dzień wolny do kalendarza firmy")
        .RequirePermission("config.manage");

        group.MapPost("/zestaw-polski", async (int? rok, ISender sender, CancellationToken ct) =>
        {
            var wynik = await sender.Send(new WstawZestawPolskiCommand(rok ?? DateTime.UtcNow.Year), ct);
            return wynik.IsSuccess ? Results.Ok(new { dodane = wynik.Value }) : wynik.ToHttpResult();
        })
        .WithName("WstawZestawPolski")
        .WithSummary("Wstawia typowe dni wolne w Polsce, pomijając już wpisane")
        .RequirePermission("config.manage");

        group.MapPut("/{id:guid}", async (
            Guid id, ZapiszDzienWolnyRequest request, ISender sender, CancellationToken ct) =>
        {
            var wynik = await sender.Send(new ZmienDzienWolnyCommand(
                id, request.Nazwa, request.Rodzaj, request.ObnizaNorme), ct);

            return wynik.IsSuccess ? Results.NoContent() : wynik.ToHttpResult();
        })
        .WithName("ZmienDzienWolny")
        .WithSummary("Zmienia opis dnia wolnego")
        .RequirePermission("config.manage");

        group.MapDelete("/{id:guid}", async (Guid id, ISender sender, CancellationToken ct) =>
        {
            var wynik = await sender.Send(new UsunDzienWolnyCommand(id), ct);
            return wynik.IsSuccess ? Results.NoContent() : wynik.ToHttpResult();
        })
        .WithName("UsunDzienWolny")
        .WithSummary("Usuwa dzień wolny z kalendarza firmy")
        .RequirePermission("config.manage");

        return endpoints;
    }
}

public sealed record ZapiszDzienWolnyRequest(
    DateOnly Data,
    string Nazwa,
    RodzajDniaWolnego Rodzaj,
    bool ObnizaNorme);

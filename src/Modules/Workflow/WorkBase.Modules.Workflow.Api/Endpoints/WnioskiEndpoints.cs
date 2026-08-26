using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using WorkBase.Modules.Workflow.Application.Commands;
using WorkBase.Modules.Workflow.Domain.Entities;
using WorkBase.Shared.Api;
using WorkBase.Shared.Auth;

namespace WorkBase.Modules.Workflow.Api.Endpoints;

/// <summary>
/// Wnioski firmowe składane na formularzach definiowanych przez administratora.
/// </summary>
/// <remarks>
/// Silnik obiegów obsługuje je tą samą drogą co wnioski urlopowe, więc akceptacja, eskalacje,
/// historia i zastępstwa działają bez żadnej dodatkowej pracy.
/// </remarks>
public static class WnioskiEndpoints
{
    public static IEndpointRouteBuilder MapWnioskiEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var typy = endpoints.MapGroup("/api/wnioski/typy")
            .WithTags("Wnioski")
            .RequireAuthorization();

        typy.MapGet("/", async (bool? wszystkie, ISender sender, CancellationToken ct) =>
        {
            // Pracownik widzi tylko aktywne — nieaktywnych i tak nie zlozy. Administrator
            // potrzebuje pelnej listy, zeby moc je z powrotem wlaczyc.
            var wynik = await sender.Send(new PobierzTypyWnioskowQuery(TylkoAktywne: wszystkie != true), ct);
            return wynik.ToHttpResult();
        })
        .WithName("PobierzTypyWnioskow")
        .WithSummary("Rodzaje wniosków dostępne w firmie")
        .RequirePermission("wnioski.view");

        typy.MapPost("/", async (ZapiszTypWnioskuRequest request, ISender sender, CancellationToken ct) =>
        {
            var wynik = await sender.Send(new UtworzTypWnioskuCommand(
                request.Kod, request.Nazwa, request.Opis, request.Pola, request.WymagaAkceptacji), ct);

            return wynik.IsSuccess
                ? Results.Created($"/api/wnioski/typy/{wynik.Value}", wynik.Value)
                : wynik.ToHttpResult();
        })
        .WithName("UtworzTypWniosku")
        .WithSummary("Definiuje nowy rodzaj wniosku")
        .RequirePermission("wnioski.manage");

        typy.MapPut("/{id:guid}", async (
            Guid id, ZapiszTypWnioskuRequest request, ISender sender, CancellationToken ct) =>
        {
            var wynik = await sender.Send(new ZmienTypWnioskuCommand(
                id, request.Nazwa, request.Opis, request.Pola,
                request.WymagaAkceptacji, request.Aktywny ?? true), ct);

            return wynik.IsSuccess ? Results.NoContent() : wynik.ToHttpResult();
        })
        .WithName("ZmienTypWniosku")
        .WithSummary("Zmienia definicję rodzaju wniosku")
        .RequirePermission("wnioski.manage");

        var wnioski = endpoints.MapGroup("/api/wnioski")
            .WithTags("Wnioski")
            .RequireAuthorization();

        wnioski.MapGet("/moje", async (ClaimsPrincipal user, ISender sender, CancellationToken ct) =>
        {
            if (user.EmployeeId() is not Guid employeeId) return Results.Forbid();

            var wynik = await sender.Send(new PobierzMojeWnioskiQuery(employeeId), ct);
            return wynik.ToHttpResult();
        })
        .WithName("PobierzMojeWnioski")
        .WithSummary("Wnioski złożone przez zalogowaną osobę")
        .RequirePermission("wnioski.view");

        // Tresc wniosku dla osoby, ktora ma o nim zdecydowac. Uprawnienie jest tu slabym
        // zabezpieczeniem (wnioski.view ma kazdy) — wlasciwe rozstrzygniecie robi zapytanie:
        // przepuszcza wnioskodawce albo akceptanta TEGO obiegu. Zakres danych sie nie nadaje,
        // bo przy zastepstwie zastepca bywa poza zakresem osoby zastepowanej.
        wnioski.MapGet("/{id:guid}", async (
            Guid id, ClaimsPrincipal user, ISender sender, CancellationToken ct) =>
        {
            if (user.EmployeeId() is not Guid employeeId) return Results.Forbid();

            var wynik = await sender.Send(new PobierzWniosekDoDecyzjiQuery(id, employeeId), ct);
            return wynik.ToHttpResult();
        })
        .WithName("PobierzWniosek")
        .WithSummary("Treść wniosku — dla wnioskodawcy albo akceptanta")
        .RequirePermission("wnioski.view");

        wnioski.MapPost("/", async (
            ZlozWniosekRequest request, ClaimsPrincipal user, ISender sender, CancellationToken ct) =>
        {
            // Wniosek sklada sie zawsze we wlasnym imieniu — identyfikator bierzemy z tokenu,
            // a nie z ciala zadania, zeby nie dalo sie zlozyc wniosku za kogos innego.
            if (user.EmployeeId() is not Guid employeeId) return Results.Forbid();

            var wynik = await sender.Send(new ZlozWniosekCommand(
                request.TypWnioskuId, employeeId, request.Wartosci), ct);

            return wynik.IsSuccess
                ? Results.Created($"/api/wnioski/{wynik.Value}", wynik.Value)
                : wynik.ToHttpResult();
        })
        .WithName("ZlozWniosek")
        .WithSummary("Składa wniosek na formularzu wybranego rodzaju")
        .RequirePermission("wnioski.create");

        wnioski.MapPost("/{id:guid}/anuluj", async (
            Guid id, ClaimsPrincipal user, ISender sender, CancellationToken ct) =>
        {
            if (user.EmployeeId() is not Guid employeeId) return Results.Forbid();

            var wynik = await sender.Send(new AnulujWniosekCommand(id, employeeId), ct);
            return wynik.IsSuccess ? Results.NoContent() : wynik.ToHttpResult();
        })
        .WithName("AnulujWniosek")
        .WithSummary("Wycofuje własny wniosek, dopóki nikt nie podjął decyzji")
        .RequirePermission("wnioski.create");

        return endpoints;
    }
}

public sealed record ZapiszTypWnioskuRequest(
    string Kod,
    string Nazwa,
    string? Opis,
    IReadOnlyList<PoleWniosku> Pola,
    bool WymagaAkceptacji,
    bool? Aktywny);

public sealed record ZlozWniosekRequest(
    Guid TypWnioskuId,
    IReadOnlyDictionary<string, string?> Wartosci);

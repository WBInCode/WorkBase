using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using WorkBase.Modules.Organization.Application.Commands.Zastepstwa;
using WorkBase.Shared.Api;
using WorkBase.Shared.Auth;

namespace WorkBase.Modules.Organization.Api.Endpoints;

/// <summary>
/// Zastępstwa w akceptacji wniosków.
/// </summary>
/// <remarks>
/// Zastępstwo wyznacza sam zainteresowany dla siebie — to jego kolejka i on wie, kiedy go nie
/// będzie. Administrator (org.manage) może ustawić je za kogoś, bo ktoś musi umieć odblokować
/// zespół, gdy kierownik zniknął bez uprzedzenia.
/// </remarks>
public static class ZastepstwaEndpoints
{
    public static IEndpointRouteBuilder MapZastepstwaEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/org/zastepstwa")
            .WithTags("Zastępstwa")
            .RequireAuthorization();

        group.MapGet("/{employeeId:guid}", async (
            Guid employeeId, ClaimsPrincipal user, IPermissionService permissions,
            ISender sender, CancellationToken ct) =>
        {
            if (!await MozeZarzadzac(user, employeeId, permissions, ct)) return Results.Forbid();

            var wynik = await sender.Send(new PobierzZastepstwaQuery(employeeId), ct);
            return wynik.ToHttpResult();
        })
        .WithName("PobierzZastepstwa")
        .WithSummary("Lista zastępstw wyznaczonych przez daną osobę");

        group.MapPost("/", async (
            WyznaczZastepstwoRequest request, ClaimsPrincipal user, IPermissionService permissions,
            ISender sender, CancellationToken ct) =>
        {
            if (!await MozeZarzadzac(user, request.ZastepowanyEmployeeId, permissions, ct))
                return Results.Forbid();

            var wynik = await sender.Send(new WyznaczZastepstwoCommand(
                request.ZastepowanyEmployeeId,
                request.ZastepcaEmployeeId,
                request.OdKiedy,
                request.DoKiedy,
                request.Powod), ct);

            return wynik.IsSuccess
                ? Results.Created($"/api/org/zastepstwa/{request.ZastepowanyEmployeeId}", wynik.Value)
                : wynik.ToHttpResult();
        })
        .WithName("WyznaczZastepstwo")
        .WithSummary("Wyznacza zastępcę na czas nieobecności");

        group.MapDelete("/{id:guid}", async (
            Guid id, ClaimsPrincipal user, IPermissionService permissions,
            ISender sender, CancellationToken ct) =>
        {
            // Sprawdzenie, czyje to zastępstwo, robi handler przez filtr najemcy; tutaj
            // wystarczy, że pytający w ogóle zarządza czyimikolwiek zastępstwami.
            var wlasnyEmployeeId = user.EmployeeId();
            if (wlasnyEmployeeId == Guid.Empty
                && !await user.HasPermissionAsync(permissions, "org.manage", ct))
            {
                return Results.Forbid();
            }

            var wynik = await sender.Send(new OdwolajZastepstwoCommand(id), ct);
            return wynik.IsSuccess ? Results.NoContent() : wynik.ToHttpResult();
        })
        .WithName("OdwolajZastepstwo")
        .WithSummary("Odwołuje wyznaczone zastępstwo");

        return endpoints;
    }

    private static async Task<bool> MozeZarzadzac(
        ClaimsPrincipal user, Guid employeeId, IPermissionService permissions, CancellationToken ct)
        => user.EmployeeId() == employeeId
           || await user.HasPermissionAsync(permissions, "org.manage", ct);
}

public sealed record WyznaczZastepstwoRequest(
    Guid ZastepowanyEmployeeId,
    Guid ZastepcaEmployeeId,
    DateOnly OdKiedy,
    DateOnly DoKiedy,
    string? Powod);

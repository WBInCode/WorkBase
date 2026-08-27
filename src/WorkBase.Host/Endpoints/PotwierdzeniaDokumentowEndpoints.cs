using System.Security.Claims;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using WorkBase.Infrastructure.Dokumenty;
using WorkBase.Shared.Api;
using WorkBase.Shared.Auth;

namespace WorkBase.Host.Endpoints;

/// <summary>
/// Potwierdzenie zapoznania się z dokumentem — regulamin, instrukcja BHP, polityka.
/// </summary>
/// <remarks>
/// Endpointy siedzą w Hoście, bo logika łączy moduł dokumentów z kartotekami pracowników
/// (<see cref="PotwierdzeniaDokumentow"/>). Ten sam wzorzec co zapisane widoki.
///
/// Raport „kto potwierdził" jest firmowy (<c>documents.manage</c>), bez zawężania zakresem danych —
/// zgodnie z decyzją dla całego modułu dokumentów zapisaną w <c>ZakresDanychPracownikaTests</c>.
/// </remarks>
public static class PotwierdzeniaDokumentowEndpoints
{
    public static IEndpointRouteBuilder MapPotwierdzeniaDokumentowEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/documents")
            .WithTags("DocumentAcknowledgements")
            .RequireAuthorization();

        group.MapGet("/do-potwierdzenia", async (
            ClaimsPrincipal user, PotwierdzeniaDokumentow serwis, CancellationToken ct) =>
        {
            // Konto bez kartoteki (np. operator) nie ma czego potwierdzac — pusta lista, nie blad.
            if (user.EmployeeId() is not Guid employeeId) return Results.Ok(Array.Empty<DokumentDoPotwierdzenia>());
            return Results.Ok(await serwis.DoPotwierdzeniaAsync(employeeId, ct));
        })
        .WithName("DokumentyDoPotwierdzenia")
        .WithSummary("Dokumenty, z którymi pytający ma się jeszcze zapoznać")
        .RequirePermission("documents.view");

        group.MapPost("/{id:guid}/potwierdz", async (
            Guid id, ClaimsPrincipal user, PotwierdzeniaDokumentow serwis, CancellationToken ct) =>
        {
            var tenantId = user.GetTenantId();
            if (tenantId is null || user.EmployeeId() is not Guid employeeId) return Results.Forbid();

            // Identyfikator pracownika z tokenu — nie da sie potwierdzic za kogos.
            return await serwis.PotwierdzAsync(id, employeeId, tenantId.Value, ct)
                ? Results.NoContent()
                : Results.NotFound();
        })
        .WithName("PotwierdzDokument")
        .WithSummary("Pracownik potwierdza, że zapoznał się z dokumentem")
        .RequirePermission("documents.view");

        group.MapPut("/{id:guid}/wymaga-potwierdzenia", async (
            Guid id, WymagaBody body, PotwierdzeniaDokumentow serwis, CancellationToken ct) =>
            await serwis.UstawWymagaAsync(id, body.Wymaga, ct)
                ? Results.NoContent()
                : Results.BadRequest(new { message = "Dokument nie istnieje albo nie ma adresata (jest przypięty do zadania, nie do osoby ani firmy)." }))
        .WithName("UstawWymagaPotwierdzenia")
        .WithSummary("Oznacza dokument jako wymagający potwierdzenia zapoznania")
        .RequirePermission("documents.create");

        group.MapGet("/{id:guid}/potwierdzenia", async (
            Guid id, PotwierdzeniaDokumentow serwis, CancellationToken ct) =>
        {
            var raport = await serwis.RaportAsync(id, ct);
            return raport is null ? Results.NotFound() : Results.Ok(raport);
        })
        .WithName("RaportPotwierdzenDokumentu")
        .WithSummary("Kto potwierdził, kto nie i od ilu dni")
        .RequirePermission("documents.manage");

        return endpoints;
    }

    public sealed record WymagaBody(bool Wymaga);
}

using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using WorkBase.Modules.Organization.Application.Commands.Terminy;
using WorkBase.Modules.Organization.Application.Queries.Terminy;
using WorkBase.Shared.Api;
using WorkBase.Shared.Auth;

namespace WorkBase.Modules.Organization.Api.Endpoints;

/// <summary>
/// Terminy pilnowane przy pracowniku: badania lekarskie, szkolenia BHP, uprawnienia
/// z datą ważności, koniec umowy.
/// </summary>
/// <remarks>
/// <para>
/// <b>Nic tu niczego nie blokuje.</b> Miniony termin nie odbiera pracownikowi możliwości
/// rejestracji czasu ani składania wniosków — pokazujemy stan i zostawiamy decyzję firmie.
/// Dopuszczenie do pracy jest odpowiedzialnością pracodawcy, nie systemu.
/// </para>
/// <para>
/// Terminy to dane wrażliwe o osobie, więc cudze widzi tylko ktoś z <c>org.view-team</c>
/// i wyłącznie w swoim zakresie danych. Własne widzi każdy — bez żadnego uprawnienia.
/// </para>
/// </remarks>
public static class TerminyEndpoints
{
    private const string PodgladZespolu = "org.view-team";
    private const string ModulZakresu = "org";

    public static IEndpointRouteBuilder MapTerminyEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/terminy")
            .WithTags("Terminy")
            .RequireAuthorization();

        // --- rodzaje terminów (słownik firmy) ---

        group.MapGet("/typy", async (bool? wszystkie, ISender sender, CancellationToken ct) =>
        {
            var wynik = await sender.Send(new PobierzTypyTerminowQuery(wszystkie ?? false), ct);
            return wynik.ToHttpResult();
        })
        .WithName("PobierzTypyTerminow")
        .WithSummary("Rodzaje terminów pilnowanych w firmie")
        .RequirePermission("org.view");

        group.MapPost("/typy", async (ZapiszTypTerminuCommand body, ISender sender, CancellationToken ct) =>
        {
            var wynik = await sender.Send(body, ct);
            return wynik.IsSuccess ? Results.Ok(new { id = wynik.Value }) : wynik.ToHttpResult();
        })
        .WithName("ZapiszTypTerminu")
        .WithSummary("Dodaje albo zmienia rodzaj terminu")
        .RequirePermission("org.edit");

        // --- terminy pracownika ---

        group.MapGet("/pracownik/{employeeId:guid}", async (
            Guid employeeId,
            bool? zArchiwalnymi,
            ClaimsPrincipal user,
            IPermissionService permissions,
            IEmployeeScopeResolver scopes,
            ISender sender,
            CancellationToken ct) =>
        {
            // Wlasne terminy widzi kazdy; cudze wymagaja uprawnienia zespolowego ORAZ zakresu.
            if (!await user.CanAccessEmployeeAsync(
                    employeeId, permissions, scopes, PodgladZespolu, ModulZakresu, ct))
            {
                return Results.Forbid();
            }

            var wynik = await sender.Send(
                new PobierzTerminyPracownikaQuery(employeeId, zArchiwalnymi ?? false), ct);
            return wynik.ToHttpResult();
        })
        .WithName("PobierzTerminyPracownika")
        .WithSummary("Terminy jednego pracownika")
        .RequirePermission("org.view");

        group.MapPost("/", async (ZapiszTerminCommand body, ISender sender, CancellationToken ct) =>
        {
            var wynik = await sender.Send(body, ct);
            return wynik.IsSuccess ? Results.Ok(new { id = wynik.Value }) : wynik.ToHttpResult();
        })
        .WithName("ZapiszTermin")
        .WithSummary("Dodaje albo zmienia termin pracownika")
        .RequirePermission("org.edit");

        group.MapPost("/{id:guid}/odnow", async (
            Guid id, OdnowTerminBody body, ISender sender, CancellationToken ct) =>
        {
            var wynik = await sender.Send(
                new OdnowTerminCommand(id, body.NowyWaznyDo, body.WykonanyDnia, body.Notatka), ct);
            return wynik.IsSuccess ? Results.Ok(new { id = wynik.Value }) : wynik.ToHttpResult();
        })
        .WithName("OdnowTermin")
        .WithSummary("Archiwizuje termin i zakłada nowy z późniejszą datą")
        .RequirePermission("org.edit");

        group.MapPost("/{id:guid}/archiwizuj", async (Guid id, ISender sender, CancellationToken ct) =>
        {
            var wynik = await sender.Send(new ZarchiwizujTerminCommand(id), ct);
            return wynik.IsSuccess ? Results.NoContent() : wynik.ToHttpResult();
        })
        .WithName("ZarchiwizujTermin")
        .WithSummary("Chowa termin z widoku, zachowując go w historii")
        .RequirePermission("org.edit");

        // --- lista zbiorcza ---

        group.MapGet("/wygasajace", async (
            int? dni,
            ClaimsPrincipal user,
            IPermissionService permissions,
            IEmployeeScopeResolver scopes,
            ISender sender,
            CancellationToken ct) =>
        {
            var wynik = await sender.Send(new PobierzWygasajaceTerminyQuery(dni ?? 30), ct);
            if (!wynik.IsSuccess) return wynik.ToHttpResult();

            // Lista niesie nazwiska, wiec zawezenie MUSI byc po stronie serwera. Wlasne pozycje
            // zostaja zawsze — FilterAccessibleEmployeesAsync przepuszcza wlasny identyfikator
            // bez pytania o uprawnienie zespolowe.
            var widoczni = await user.FilterAccessibleEmployeesAsync(
                wynik.Value.Select(t => t.EmployeeId).Distinct().ToList(),
                permissions, scopes, PodgladZespolu, ModulZakresu, ct);

            return Results.Ok(wynik.Value.Where(t => widoczni.Contains(t.EmployeeId)).ToList());
        })
        .WithName("PobierzWygasajaceTerminy")
        .WithSummary("Co wygasa w najbliższych dniach, w zakresie danych pytającego")
        .RequirePermission("org.view");

        return endpoints;
    }

    public sealed record OdnowTerminBody(DateOnly NowyWaznyDo, DateOnly? WykonanyDnia, string? Notatka);
}

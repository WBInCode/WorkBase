using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using WorkBase.Modules.Organization.Application.Commands.Mienie;
using WorkBase.Shared.Api;
using WorkBase.Shared.Auth;

namespace WorkBase.Modules.Organization.Api.Endpoints;

/// <summary>
/// Mienie powierzone: co firma wydała pracownikowi i co ma wrócić, gdy odchodzi.
/// </summary>
/// <remarks>
/// <para>
/// Ten sam model dostępu co terminy kadrowe: własne wpisy widzi każdy bez żadnego uprawnienia,
/// cudze — ktoś z <c>org.view-team</c> w swoim zakresie danych. Wydanie i zwrot to <c>org.edit</c>.
/// </para>
/// <para>
/// Potwierdzenie odbioru składa wyłącznie pracownik, ze swojego konta. Identyfikator bierzemy
/// z tokenu, nie z ciała żądania — inaczej dałoby się potwierdzić za kogoś, a wtedy
/// potwierdzenie nie znaczyłoby nic.
/// </para>
/// </remarks>
public static class MienieEndpoints
{
    private const string PodgladZespolu = "org.view-team";
    private const string ModulZakresu = "org";

    public static IEndpointRouteBuilder MapMienieEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/mienie")
            .WithTags("Mienie")
            .RequireAuthorization();

        group.MapGet("/pracownik/{employeeId:guid}", async (
            Guid employeeId,
            bool? zeZwroconymi,
            ClaimsPrincipal user,
            IPermissionService permissions,
            IEmployeeScopeResolver scopes,
            ISender sender,
            CancellationToken ct) =>
        {
            if (!await user.CanAccessEmployeeAsync(
                    employeeId, permissions, scopes, PodgladZespolu, ModulZakresu, ct))
            {
                return Results.Forbid();
            }

            var wynik = await sender.Send(new PobierzMieniePracownikaQuery(employeeId, zeZwroconymi ?? false), ct);
            return wynik.ToHttpResult();
        })
        .WithName("PobierzMieniePracownika")
        .WithSummary("Rzeczy wydane jednemu pracownikowi")
        .RequirePermission("org.view");

        group.MapGet("/pracownik/{employeeId:guid}/niezwrocone", async (
            Guid employeeId,
            ClaimsPrincipal user,
            IPermissionService permissions,
            IEmployeeScopeResolver scopes,
            ISender sender,
            CancellationToken ct) =>
        {
            if (!await user.CanAccessEmployeeAsync(
                    employeeId, permissions, scopes, PodgladZespolu, ModulZakresu, ct))
            {
                return Results.Forbid();
            }

            var wynik = await sender.Send(new PoliczNiezwroconeQuery(employeeId), ct);
            return wynik.IsSuccess ? Results.Ok(new { liczba = wynik.Value }) : wynik.ToHttpResult();
        })
        .WithName("PoliczNiezwroconeMienie")
        .WithSummary("Ile rzeczy pracownik ma jeszcze oddać — do ostrzeżenia przy dezaktywacji")
        .RequirePermission("org.view");

        group.MapPost("/", async (WydajMienieCommand body, ISender sender, CancellationToken ct) =>
        {
            var wynik = await sender.Send(body, ct);
            return wynik.IsSuccess ? Results.Ok(new { id = wynik.Value }) : wynik.ToHttpResult();
        })
        .WithName("WydajMienie")
        .WithSummary("Zapisuje wydanie rzeczy pracownikowi albo poprawia wpis")
        .RequirePermission("org.edit");

        group.MapPost("/{id:guid}/zwrot", async (
            Guid id, ZwrotBody body, ISender sender, CancellationToken ct) =>
        {
            var wynik = await sender.Send(new ZwrocMienieCommand(id, body.ZwroconoDnia, body.Notatka), ct);
            return wynik.IsSuccess ? Results.NoContent() : wynik.ToHttpResult();
        })
        .WithName("ZwrocMienie")
        .WithSummary("Odnotowuje zwrot rzeczy do firmy")
        .RequirePermission("org.edit");

        // Bez RequirePermission: to czynnosc pracownika wobec wlasnego wpisu, a rola Pracownik
        // nie ma zadnego uprawnienia z modulu org poza org.view. Wlasnosc sprawdza handler.
        group.MapPost("/{id:guid}/potwierdz", async (
            Guid id, ClaimsPrincipal user, ISender sender, CancellationToken ct) =>
        {
            if (user.EmployeeId() is not Guid employeeId)
                return Results.Forbid();

            var wynik = await sender.Send(new PotwierdzOdbiorMieniaCommand(id, employeeId), ct);
            return wynik.IsSuccess ? Results.NoContent() : wynik.ToHttpResult();
        })
        .WithName("PotwierdzOdbiorMienia")
        .WithSummary("Pracownik potwierdza odbiór rzeczy — wyłącznie własnej");

        group.MapGet("/do-zwrotu", async (
            ClaimsPrincipal user,
            IPermissionService permissions,
            IEmployeeScopeResolver scopes,
            ISender sender,
            CancellationToken ct) =>
        {
            var wynik = await sender.Send(new PobierzMienieDoZwrotuQuery(), ct);
            if (!wynik.IsSuccess) return wynik.ToHttpResult();

            // Lista niesie nazwiska — zawezenie musi byc po stronie serwera.
            var widoczni = await user.FilterAccessibleEmployeesAsync(
                wynik.Value.Select(m => m.EmployeeId).Distinct().ToList(),
                permissions, scopes, PodgladZespolu, ModulZakresu, ct);

            return Results.Ok(wynik.Value.Where(m => widoczni.Contains(m.EmployeeId)).ToList());
        })
        .WithName("PobierzMienieDoZwrotu")
        .WithSummary("Niezwrócone rzeczy u osób, które odchodzą albo odeszły — w zakresie pytającego")
        .RequirePermission("org.view");

        return endpoints;
    }

    public sealed record ZwrotBody(DateOnly ZwroconoDnia, string? Notatka);
}

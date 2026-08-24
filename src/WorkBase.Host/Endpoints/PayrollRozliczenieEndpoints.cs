using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using WorkBase.Infrastructure.Persistence;
using WorkBase.Modules.Organization.Domain.Entities;
using WorkBase.Modules.TimeTracking.Application.Queries.Rozliczenia;
using WorkBase.Shared.Api;
using WorkBase.Shared.Auth;
using WorkBase.Shared.Domain;

namespace WorkBase.Host.Endpoints;

/// <summary>
/// Rozliczenie czasu pracy na kwoty — liczone po stronie serwera.
/// </summary>
/// <remarks>
/// Wczesniej liczyla to przegladarka z sumy godzin na karcie czasu, a zakres pracownikow
/// filtrowala u siebie. Mialo to trzy skutki: nie dalo sie zastosowac dodatku nocnego
/// (potrzebne sa wpisy, nie suma) ani swiatecznego (potrzebny kalendarz dni wolnych),
/// wzoru nie dalo sie przetestowac, a filtrowanie po stronie klienta nie chronilo danych —
/// API i tak oddawalo wszystko.
/// </remarks>
public static class PayrollRozliczenieEndpoints
{
    /// <summary>Uprawnienie do ogladania rozliczen innych osob niz wlasne.</summary>
    private const string PodgladZespolu = "payroll.view-team";

    /// <summary>
    /// Zakres liczymy wg modulu "org", nie "payroll" — payroll nie jest modulem z ModuleCatalog
    /// i nie ma dla niego wierszy w iam_data_scopes, wiec kazdy spadlby do poziomu domyslnego.
    /// </summary>
    private const string ModulZakresu = "org";

    public static IEndpointRouteBuilder MapPayrollRozliczenieEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/api/payroll/rozliczenie", async (
            [FromQuery] DateOnly od,
            // Nazwa w adresie to "do" — w C# to slowo kluczowe, wiec parametr nazywa sie inaczej.
            [FromQuery(Name = "do")] DateOnly doDnia,
            ClaimsPrincipal user,
            WorkBaseDbContext db,
            ITenantConfigService konfiguracja,
            IPermissionService permissions,
            IEmployeeScopeResolver scopes,
            ISender sender,
            CancellationToken ct) =>
        {
            var tenantId = user.GetTenantId();
            if (tenantId is null) return Results.Forbid();

            if (doDnia < od)
                return Results.BadRequest(new { message = "Data końcowa jest wcześniejsza niż początkowa." });

            // Okres dluzszy niz rok liczylby sie doba po dobie dla kazdego pracownika i zjadalby
            // pamiec bez zadnego pozytku — kadry rozliczaja miesiac.
            if (doDnia.DayNumber - od.DayNumber > 366)
                return Results.BadRequest(new { message = "Okres nie może być dłuższy niż rok." });

            var wszyscy = await db.Set<Employee>()
                .Where(e => e.Status == EmployeeStatus.Active)
                .Select(e => new { e.Id, e.HourlyRate })
                .ToListAsync(ct);

            var widoczni = await user.FilterAccessibleEmployeesAsync(
                wszyscy.Select(e => e.Id).ToList(), permissions, scopes, PodgladZespolu, ModulZakresu, ct);

            var stawki = wszyscy
                .Where(e => widoczni.Contains(e.Id))
                .ToDictionary(e => e.Id, e => e.HourlyRate ?? 0m);

            var (nadgodziny, nocny, swiateczny) =
                await PayrollSettingsEndpoints.PobierzMnoznikiAsync(konfiguracja, tenantId.Value, ct);
            var (nocOd, nocDo) =
                await PayrollSettingsEndpoints.PobierzPoreNocnaAsync(konfiguracja, tenantId.Value, ct);

            var wynik = await sender.Send(new PobierzRozliczenieQuery(
                od, doDnia, stawki, nadgodziny, nocny, swiateczny, nocOd, nocDo), ct);

            return wynik.ToHttpResult();
        })
        .WithName("PobierzRozliczenie")
        .WithSummary("Rozliczenie czasu pracy za okres, w zakresie danych pytającego")
        .RequirePermission("payroll.view");

        return endpoints;
    }
}

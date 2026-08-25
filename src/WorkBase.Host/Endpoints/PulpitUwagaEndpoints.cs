using System.Security.Claims;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using WorkBase.Infrastructure.Persistence;
using WorkBase.Modules.Dashboard.Application.Contracts;
using WorkBase.Modules.Organization.Domain.Entities;
using WorkBase.Shared.Auth;

namespace WorkBase.Host.Endpoints;

/// <summary>
/// „Co wymaga mojej uwagi” — lista rzeczy do zrobienia, a nie kafelki z liczbami.
/// </summary>
/// <remarks>
/// Pulpit pokazywał dotąd liczby: obecni, spóźnieni, otwarte zadania. Kierownik rzadko
/// potrzebuje liczby — potrzebuje wiedzieć, czym zająć się dziś rano i w jakiej kolejności.
/// Wszystkie pozycje wynikają z danych, które system już ma.
///
/// Zakres liczony jest tak samo jak przy stawkach godzinowych: przez uprawnienie zespołowe
/// i zakres danych modułu org. Panel wypisuje nazwiska, więc zawężenie musi być po stronie
/// serwera — inaczej byłoby to samo, co robiła strona wynagrodzeń przed poprawką.
/// </remarks>
public static class PulpitUwagaEndpoints
{
    private const string PodgladZespolu = "time.view-team";
    private const string PodgladWynagrodzen = "payroll.view-team";
    private const string ModulZakresu = "org";

    /// <summary>Po ilu dniach bez decyzji wniosek trafia na listę. Próg celowo niski.</summary>
    private const int DniOczekiwaniaNaDecyzje = 2;

    public static IEndpointRouteBuilder MapPulpitUwagaEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/api/dashboard/uwaga", async (
            ClaimsPrincipal user,
            WorkBaseDbContext db,
            IPermissionService permissions,
            IEmployeeScopeResolver scopes,
            IAlertyQueryService alerty,
            CancellationToken ct) =>
        {
            var tenantId = user.GetTenantId();
            if (tenantId is null) return Results.Forbid();

            var mojEmployeeId = user.EmployeeId();

            var wszyscy = await db.Set<Employee>()
                .Where(e => e.Status == EmployeeStatus.Active)
                .Select(e => e.Id)
                .ToListAsync(ct);

            // Bez uprawnienia zespolowego zostaja wylacznie pozycje osobiste — czyli wnioski
            // czekajace na decyzje tego uzytkownika. Akceptanta wyznacza relacja przelozonego,
            // a nie uprawnienie, wiec szeregowy pracownik tez moze je mieć.
            var maZespol = await user.HasPermissionAsync(permissions, PodgladZespolu, ct);

            var wZakresie = maZespol
                ? await user.FilterAccessibleEmployeesAsync(
                    wszyscy, permissions, scopes, PodgladZespolu, ModulZakresu, ct)
                : new HashSet<Guid>();

            var pokazujStawki = await user.HasPermissionAsync(permissions, PodgladWynagrodzen, ct);

            var wynik = await alerty.PobierzAsync(
                tenantId.Value,
                wZakresie.ToList(),
                mojEmployeeId == Guid.Empty ? null : mojEmployeeId,
                DniOczekiwaniaNaDecyzje,
                pokazujStawki,
                ct);

            return Results.Ok(wynik);
        })
        .WithName("PobierzAlertyPulpitu")
        .WithSummary("Pozycje wymagające uwagi, w zakresie danych pytającego")
        .RequirePermission("dashboard.view");

        return endpoints;
    }
}

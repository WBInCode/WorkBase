using System.Security.Claims;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using WorkBase.Infrastructure.Setup;
using WorkBase.Shared.Auth;

namespace WorkBase.Host.Endpoints;

/// <summary>
/// Kreator pierwszego startu. Projekt: docs/KONFIGURATOR-PIERWSZEGO-STARTU.md.
/// Na razie sam stan i domkniecie — kroki (ludzie, godziny pracy, akceptanci) dochodza dalej.
/// </summary>
public static class SetupEndpoints
{
    public static IEndpointRouteBuilder MapSetupEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/setup")
            .WithTags("Konfiguracja startowa")
            .RequireAuthorization();

        group.MapGet("/state", async (
            ClaimsPrincipal user,
            IKonfiguracjaStartowaService konfiguracja,
            CancellationToken ct) =>
        {
            var tenantId = user.GetTenantId();
            if (tenantId is null) return Results.Forbid();

            var stan = await konfiguracja.PobierzAsync(tenantId.Value, ct);
            return Results.Ok(new
            {
                wymagana = stan.Wymagana,
                ukonczona = stan.UkonczonaO is not null,
                ukonczonaO = stan.UkonczonaO,
            });
        })
        .WithName("PobierzStanKonfiguracji")
        .WithSummary("Stan kreatora pierwszego startu");

        // Bez RequirePermission: kreator prowadzi wlasciciel firmy zaraz po nadaniu licencji,
        // a uprawnienia nadaje sie dopiero w jego trakcie. Rola wlasciciela przychodzi z Huba
        // (hub_role = owner -> Admin), wiec zabezpieczeniem jest tu identity.manage tam, gdzie
        // istnieje, a na tym etapie — sam fakt zalogowania do firmy, ktora czeka na konfiguracje.
        group.MapPost("/complete", async (
            ClaimsPrincipal user,
            IKonfiguracjaStartowaService konfiguracja,
            CancellationToken ct) =>
        {
            var tenantId = user.GetTenantId();
            if (tenantId is null) return Results.Forbid();

            await konfiguracja.UkonczAsync(tenantId.Value, ct);
            return Results.NoContent();
        })
        .WithName("ZakonczKonfiguracje")
        .WithSummary("Oznacza konfigurację pierwszego startu jako ukończoną");

        return endpoints;
    }
}

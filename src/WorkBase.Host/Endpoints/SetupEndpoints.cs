using System.Security.Claims;
using System.Text.Json;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using WorkBase.Infrastructure.Setup;
using WorkBase.Modules.Organization.Application.Commands.Employees;
using WorkBase.Modules.Organization.Application.Queries.Employees;
using WorkBase.Modules.TimeTracking.Application.Commands;
using WorkBase.Modules.TimeTracking.Domain.Entities;
using WorkBase.Shared.Api;
using WorkBase.Shared.Auth;

namespace WorkBase.Host.Endpoints;

/// <summary>
/// Kreator pierwszego startu. Projekt: docs/KONFIGURATOR-PIERWSZEGO-STARTU.md.
///
/// Cztery ekrany, trzy pytania: kto tu pracuje, w jakich godzinach, kto akceptuje. Kazdy krok
/// jest CIENKA WARSTWA nad komenda, ktorej uzywa panel administratora — nie druga sciezka
/// tworzenia danych. Inaczej za pol roku byloby to dwa sposoby zakladania pracownika, z ktorych
/// jeden ma blad.
///
/// Wszystkie odczyty potrzebne kreatorowi musza byc TUTAJ: reszta API jest za bramka 409
/// (KonfiguracjaStartowaMiddleware), wiec kreator nie moze siegnac do /api/org.
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
                aktualnyKrok = stan.AktualnyKrok,
                pominieteKroki = stan.PominieteKroki ?? [],
                kroki = KonfiguracjaStartowa.Kroki.WKolejnosci,
            });
        })
        .WithName("PobierzStanKonfiguracji")
        .WithSummary("Stan kreatora pierwszego startu");

        // Ekran akceptantow musi pokazac, komu ustawiamy przelozonego. /api/org/employees jest
        // za bramka, wiec lista idzie stad — w wersji okrojonej. Swiadomie BEZ stawki godzinowej:
        // pelne EmployeeDto ja niesie, a kreator nie ma powodu jej pokazywac.
        group.MapGet("/employees", async (
            ISender sender,
            CancellationToken ct) =>
        {
            var wynik = await sender.Send(new GetEmployeesQuery(null, null, null, Page: 1, PageSize: 500), ct);
            if (!wynik.IsSuccess) return wynik.ToHttpResult();

            var osoby = wynik.Value.Items
                .Select(o => new { id = o.Id, imie = o.FirstName, nazwisko = o.LastName, email = o.Email })
                .ToList();

            return Results.Ok(osoby);
        })
        .WithName("PobierzPracownikowKreatora")
        .WithSummary("Lista pracowników na potrzeby kreatora");

        group.MapPost("/employees", async (
            LudzieBody body,
            ClaimsPrincipal user,
            ISender sender,
            IKonfiguracjaStartowaService konfiguracja,
            CancellationToken ct) =>
        {
            var tenantId = user.GetTenantId();
            if (tenantId is null) return Results.Forbid();

            var wiersze = (body.Pracownicy ?? [])
                .Select(o => new ImportEmployeeRow(
                    o.Imie, o.Nazwisko, o.Email, o.Numer,
                    o.DataZatrudnienia ?? DateTime.UtcNow.Date))
                .ToList();

            // „Na razie tylko ja" to pusta lista — poprawny wybor, nie bledny stan. Krok
            // zapisujemy jako pominiety, zeby ekran podsumowania mogl o tym powiedziec.
            if (wiersze.Count == 0)
            {
                await konfiguracja.ZapiszKrokAsync(
                    tenantId.Value, KonfiguracjaStartowa.Kroki.Ludzie, pominiety: true, ct);
                return Results.Ok(new { dodani = 0, pominieci = 0, bledy = Array.Empty<string>() });
            }

            var wynik = await sender.Send(
                new ImportEmployeesCommand(wiersze, ZapraszajDoHuba: body.ZaprosicTeraz), ct);
            if (!wynik.IsSuccess) return wynik.ToHttpResult();

            await konfiguracja.ZapiszKrokAsync(tenantId.Value, KonfiguracjaStartowa.Kroki.Ludzie, ct: ct);

            return Results.Ok(new
            {
                dodani = wynik.Value.Imported,
                pominieci = wynik.Value.Skipped,
                bledy = wynik.Value.Errors,
            });
        })
        .WithName("KreatorDodajPracownikow")
        .WithSummary("Krok 1 kreatora — kto tu pracuje");

        group.MapPost("/working-hours", async (
            GodzinyPracyBody body,
            ClaimsPrincipal user,
            ISender sender,
            IKonfiguracjaStartowaService konfiguracja,
            CancellationToken ct) =>
        {
            var tenantId = user.GetTenantId();
            if (tenantId is null) return Results.Forbid();

            var dni = body.DniTygodnia is { Length: > 0 } ? body.DniTygodnia : DomyslneDniRobocze;
            if (dni.Any(d => d is < 1 or > 7))
                return Results.BadRequest(new { blad = "Dzień tygodnia musi być liczbą od 1 (poniedziałek) do 7 (niedziela)." });

            var zmiany = body.Zmiany is { Count: > 0 } ? body.Zmiany : [DomyslnaZmiana];
            foreach (var zmiana in zmiany)
            {
                if (zmiana.Do <= zmiana.Od)
                    return Results.BadRequest(new { blad = $"Zmiana „{zmiana.Nazwa}” kończy się przed rozpoczęciem." });

                await sender.Send(new CreateScheduleTemplateCommand(
                    zmiana.Nazwa,
                    WzorTygodnia(dni, zmiana),
                    "Utworzone przez kreator pierwszego startu"), ct);
            }

            if (body.MinutPrzerwy > 0)
            {
                await sender.Send(new CreateBreakPolicyCommand(
                    Name: $"Przerwa {body.MinutPrzerwy} min",
                    BreakType: body.PrzerwaPlatna ? BreakType.Paid : BreakType.Unpaid,
                    MaxPerDay: 1,
                    MaxMinutesPerBreak: body.MinutPrzerwy,
                    MaxMinutesPerDay: body.MinutPrzerwy), ct);
            }

            await konfiguracja.ZapiszKrokAsync(tenantId.Value, KonfiguracjaStartowa.Kroki.Godziny, ct: ct);

            return Results.Ok(new { szablonow = zmiany.Count, przerwaMinut = body.MinutPrzerwy });
        })
        .WithName("KreatorGodzinyPracy")
        .WithSummary("Krok 2 kreatora — w jakich godzinach pracujecie");

        group.MapPost("/approvals", async (
            AkceptanciBody body,
            ClaimsPrincipal user,
            ISender sender,
            IKonfiguracjaStartowaService konfiguracja,
            CancellationToken ct) =>
        {
            var tenantId = user.GetTenantId();
            if (tenantId is null) return Results.Forbid();

            var przypisania = body.Przypisania ?? [];

            // Wariant domyslny „wszyscy do jednej osoby": zamieniamy go na te same pary,
            // co wariant reczny, zeby dalej byla jedna sciezka zapisu.
            if (przypisania.Count == 0 && body.AkceptantId is Guid akceptant)
            {
                przypisania = (body.PracownicyIds ?? [])
                    .Where(id => id != akceptant)
                    .Select(id => new Przypisanie(id, akceptant))
                    .ToList();
            }

            var ustawione = 0;
            var bledy = new List<string>();
            foreach (var para in przypisania)
            {
                var wynik = await sender.Send(
                    new SetSupervisorCommand(para.PracownikId, para.PrzelozonyId), ct);
                if (wynik.IsSuccess) ustawione++;
                else bledy.Add(wynik.Error.Message);
            }

            await konfiguracja.ZapiszKrokAsync(
                tenantId.Value,
                KonfiguracjaStartowa.Kroki.Akceptanci,
                pominiety: przypisania.Count == 0,
                ct);

            return Results.Ok(new { ustawione, bledy });
        })
        .WithName("KreatorAkceptanci")
        .WithSummary("Krok 3 kreatora — kto akceptuje wnioski");

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

    private static readonly int[] DomyslneDniRobocze = [1, 2, 3, 4, 5];

    private static readonly ZmianaBody DomyslnaZmiana =
        new("Podstawowa 8:00-16:00", new TimeOnly(8, 0), new TimeOnly(16, 0));

    /// <summary>
    /// Format zgodny z tym, ktorego uzywaja szablony grafiku w reszcie systemu
    /// (patrz DemoDataSeeder) — kreator nie wprowadza wlasnego dialektu.
    /// </summary>
    private static string WzorTygodnia(int[] dni, ZmianaBody zmiana) =>
        JsonSerializer.Serialize(dni.Order().Select(d => new
        {
            DayOfWeek = d,
            PlannedStart = zmiana.Od.ToString("HH:mm:ss"),
            PlannedEnd = zmiana.Do.ToString("HH:mm:ss"),
            ShiftType = zmiana.Nazwa,
        }));

    public sealed record LudzieBody(List<OsobaBody>? Pracownicy, bool ZaprosicTeraz = false);

    public sealed record OsobaBody(
        string Imie,
        string Nazwisko,
        string Email,
        string? Numer,
        DateTime? DataZatrudnienia);

    public sealed record GodzinyPracyBody(
        List<ZmianaBody>? Zmiany,
        int[]? DniTygodnia,
        int MinutPrzerwy = 30,
        bool PrzerwaPlatna = false);

    public sealed record ZmianaBody(string Nazwa, TimeOnly Od, TimeOnly Do);

    public sealed record AkceptanciBody(
        Guid? AkceptantId,
        List<Guid>? PracownicyIds,
        List<Przypisanie>? Przypisania);

    public sealed record Przypisanie(Guid PracownikId, Guid PrzelozonyId);
}

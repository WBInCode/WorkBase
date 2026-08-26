using System.Security.Claims;
using System.Text.Json;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using WorkBase.Contracts;
using WorkBase.Infrastructure.Setup;
using WorkBase.Modules.Organization.Application.Commands.Employees;
using WorkBase.Modules.Leave.Application.Commands;
using WorkBase.Modules.Leave.Application.Queries;
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
        .WithSummary("Lista pracowników na potrzeby kreatora")
        .RequirePermission("org.view");

        group.MapPost("/employees", async (
            LudzieBody body,
            ClaimsPrincipal user,
            ISender sender,
            IKonfiguracjaStartowaService konfiguracja,
            IKeycloakAdminService keycloak,
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
                var kartotekaSam = await ZadbajOKartotekeWlascicielaAsync(user, sender, keycloak, ct);
                await konfiguracja.ZapiszKrokAsync(
                    tenantId.Value, KonfiguracjaStartowa.Kroki.Ludzie, pominiety: true, ct);
                return Results.Ok(new
                {
                    dodani = 0,
                    pominieci = 0,
                    bledy = Array.Empty<string>(),
                    kartotekaWlasciciela = kartotekaSam,
                });
            }

            var wynik = await sender.Send(
                new ImportEmployeesCommand(wiersze, ZapraszajDoHuba: body.ZaprosicTeraz), ct);
            if (!wynik.IsSuccess) return wynik.ToHttpResult();

            var kartoteka = await ZadbajOKartotekeWlascicielaAsync(user, sender, keycloak, ct);
            await konfiguracja.ZapiszKrokAsync(tenantId.Value, KonfiguracjaStartowa.Kroki.Ludzie, ct: ct);

            return Results.Ok(new
            {
                dodani = wynik.Value.Imported,
                pominieci = wynik.Value.Skipped,
                bledy = wynik.Value.Errors,
                kartotekaWlasciciela = kartoteka,
            });
        })
        .WithName("KreatorDodajPracownikow")
        .WithSummary("Krok 1 kreatora — kto tu pracuje")
        .RequirePermission("org.create");

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
        .WithSummary("Krok 2 kreatora — w jakich godzinach pracujecie")
        .RequirePermission("time.manage");

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
        .WithSummary("Krok 3 kreatora — kto akceptuje wnioski")
        .RequirePermission("org.edit");

        // Wymiar urlopu wypoczynkowego. Seeder wpisuje nowej firmie 26 dni i dotad nikt sie
        // o tym nie dowiadywal — a to jest ustawienie FIRMY, nie nasze. Kodeks pracy przewiduje
        // 20 albo 26 dni zaleznie od stazu; pokazujemy te informacje, ale niczego nie wymuszamy
        // i nie sprawdzamy, co firma wpisze.
        group.MapGet("/leave", async (ISender sender, CancellationToken ct) =>
        {
            var typy = await sender.Send(new GetLeaveTypesQuery(), ct);
            if (!typy.IsSuccess) return typy.ToHttpResult();

            var wypoczynkowy = typy.Value.FirstOrDefault(t => t.Code == KodUrlopuWypoczynkowego);
            return Results.Ok(new { dniUrlopu = wypoczynkowy?.DefaultDaysPerYear });
        })
        .WithName("KreatorPobierzWymiarUrlopu")
        .WithSummary("Aktualny wymiar urlopu wypoczynkowego")
        .RequirePermission("leave.view");

        group.MapPost("/leave", async (
            UrlopBody body,
            ClaimsPrincipal user,
            ISender sender,
            IKonfiguracjaStartowaService konfiguracja,
            CancellationToken ct) =>
        {
            var tenantId = user.GetTenantId();
            if (tenantId is null) return Results.Forbid();

            if (body.DniUrlopu is < 0 or > 365)
                return Results.BadRequest(new { blad = "Liczba dni urlopu musi mieścić się między 0 a 365." });

            var typy = await sender.Send(new GetLeaveTypesQuery(), ct);
            if (!typy.IsSuccess) return typy.ToHttpResult();

            var wypoczynkowy = typy.Value.FirstOrDefault(t => t.Code == KodUrlopuWypoczynkowego);
            if (wypoczynkowy is null)
            {
                // Firma skasowala ten typ urlopu — jej prawo. Nie odtwarzamy go za nia.
                await konfiguracja.ZapiszKrokAsync(
                    tenantId.Value, KonfiguracjaStartowa.Kroki.Urlop, pominiety: true, ct);
                return Results.Ok(new { dniUrlopu = (int?)null, pominiety = true });
            }

            var zmiana = await sender.Send(new UpdateLeaveTypeCommand(
                wypoczynkowy.Id,
                wypoczynkowy.Code,
                wypoczynkowy.Name,
                wypoczynkowy.Description,
                wypoczynkowy.IsPaid,
                wypoczynkowy.RequiresApproval,
                body.DniUrlopu,
                wypoczynkowy.Color,
                wypoczynkowy.SortOrder), ct);
            if (!zmiana.IsSuccess) return zmiana.ToHttpResult();

            await konfiguracja.ZapiszKrokAsync(tenantId.Value, KonfiguracjaStartowa.Kroki.Urlop, ct: ct);
            return Results.Ok(new { dniUrlopu = body.DniUrlopu, pominiety = false });
        })
        .WithName("KreatorWymiarUrlopu")
        .WithSummary("Krok 4 kreatora — wymiar urlopu wypoczynkowego")
        .RequirePermission("leave.manage");

        // Kroki zapisujace maja te same uprawnienia, co ich odpowiedniki w panelu administratora
        // (org.create / time.manage / org.edit) — kreator jest cienka warstwa nad tymi samymi
        // komendami, wiec nie ma powodu, zeby wpuszczal dalej. Role sa zasiewane przy TWORZENIU
        // firmy, jeszcze przed pierwszym logowaniem, wiec wlasciciel przychodzacy z Huba jako
        // Admin ma je od poczatku. Szeregowy pracownik nie ma org.create ani org.edit.
        //
        // To jedno wywolanie zostaje OTWARTE swiadomie. Zdjecie blokady niczego nie niszczy —
        // firma i tak ma komplet domyslnych z provisioningu — a zamkniecie go zamienia kazda
        // pomylke w przypisaniu roli wlascicielowi w trwale zablokowana firme bez wyjscia.
        // Interfejs i tak pokazuje przycisk tylko osobie, ktora moze cokolwiek skonfigurowac.
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

    /// <summary>
    /// Zapewnia wlascicielowi kartoteke pracownika i wpisuje jej identyfikator do Keycloaka.
    /// </summary>
    /// <remarks>
    /// Wlasciciel loguje sie z Huba i dostaje konto uzytkownika, ale NIE kartoteke pracownika.
    /// SSO dopasowuje kartoteke po adresie e-mail (HubEmployeeIdentityLinker), wiec dopoki jej
    /// nie ma, token nie niesie employee_id — a bez tego claimu nie da sie zarejestrowac czasu
    /// pracy ani zlozyc wniosku urlopowego. Wariant „na razie tylko ja" obiecywal dzialajaca
    /// firme jednoosobowa i tej obietnicy nie dotrzymywal: wyszlo to dopiero przy przejsciu
    /// onboardingu od zera na produkcji.
    ///
    /// Atrybut w Keycloaku ustawiamy od razu (scalanie, nie nadpisanie), zeby claim pojawil sie
    /// po odswiezeniu tokenu, a nie dopiero przy kolejnym przejsciu przez handoff z Huba.
    /// Kreator konczy sie ponownym logowaniem wlasnie po to.
    /// </remarks>
    private static async Task<Guid?> ZadbajOKartotekeWlascicielaAsync(
        ClaimsPrincipal user,
        ISender sender,
        IKeycloakAdminService keycloak,
        CancellationToken ct)
    {
        var email = user.FindFirstValue("email");
        if (string.IsNullOrWhiteSpace(email)) return null;

        var znalezione = await sender.Send(new GetEmployeesQuery(email, null, null, Page: 1, PageSize: 5), ct);
        var istniejaca = znalezione.IsSuccess
            ? znalezione.Value.Items.FirstOrDefault(o => string.Equals(o.Email, email, StringComparison.OrdinalIgnoreCase))
            : null;

        Guid employeeId;
        if (istniejaca is not null)
        {
            employeeId = istniejaca.Id;
        }
        else
        {
            var utworzenie = await sender.Send(new CreateEmployeeCommand(
                user.FindFirstValue("given_name") ?? "Wlasciciel",
                user.FindFirstValue("family_name") ?? "firmy",
                email,
                EmployeeNumber: null,
                HireDate: DateTime.UtcNow.Date,
                // Bez zaproszenia: wlasciciel wlasnie jest zalogowany, wiec konto ma.
                ZapraszajDoHuba: false), ct);
            if (!utworzenie.IsSuccess) return null;
            employeeId = utworzenie.Value;
        }

        var sub = user.FindFirstValue("sub");
        if (!string.IsNullOrWhiteSpace(sub))
        {
            await keycloak.SetUserAttributesAsync(
                sub,
                new Dictionary<string, string> { ["employee_id"] = employeeId.ToString() },
                ct);
        }

        return employeeId;
    }

    /// <summary>Kod z LeaveSeeder — ten sam, ktory dostaje kazda nowa firma.</summary>
    private const string KodUrlopuWypoczynkowego = "ANNUAL";

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

    public sealed record UrlopBody(int DniUrlopu);
}

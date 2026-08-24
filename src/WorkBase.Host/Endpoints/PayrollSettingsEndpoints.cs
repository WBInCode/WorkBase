using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using System.Globalization;
using WorkBase.Shared.Auth;
using WorkBase.Shared.Domain;

namespace WorkBase.Host.Endpoints;

public static class PayrollSettingsEndpoints
{
    private const string OvertimeMultiplierKey = "payroll.overtime_multiplier";
    private const string NightMultiplierKey = "payroll.night_multiplier";
    private const string HolidayMultiplierKey = "payroll.holiday_multiplier";

    // Pore nocna wyznacza firma — system nie zna zadnego progu ustawowego i niczego nie narzuca.
    // Wartosci startowe sa tylko punktem wyjscia do zmiany.
    private const string NocOdKey = "payroll.noc_od";
    private const string NocDoKey = "payroll.noc_do";
    private static readonly TimeOnly DomyslnaNocOd = new(22, 0);
    private static readonly TimeOnly DomyslnaNocDo = new(6, 0);

    public static IEndpointRouteBuilder MapPayrollSettingsEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/payroll/settings")
            .WithTags("PayrollSettings")
            .RequireAuthorization();

        group.MapGet("/", async (ITenantConfigService config, HttpContext http) =>
        {
            var tenantId = http.User.GetTenantId();
            if (tenantId is null) return Results.Forbid();

            var ot = await config.GetAsync(tenantId.Value, OvertimeMultiplierKey);
            var night = await config.GetAsync(tenantId.Value, NightMultiplierKey);
            var holiday = await config.GetAsync(tenantId.Value, HolidayMultiplierKey);

            var nocOd = await config.GetAsync(tenantId.Value, NocOdKey);
            var nocDo = await config.GetAsync(tenantId.Value, NocDoKey);

            return Results.Ok(new PayrollSettingsDto(
                Parse(ot, 1.5m),
                Parse(night, 1.2m),
                Parse(holiday, 2.0m),
                ParseGodzine(nocOd, DomyslnaNocOd).ToString("HH:mm"),
                ParseGodzine(nocDo, DomyslnaNocDo).ToString("HH:mm")));
        })
        .WithName("GetPayrollSettings")
        .WithSummary("Pobierz ustawienia naliczania wynagrodzeń (mnożniki)");

        group.MapPut("/", async (
            UpdatePayrollSettingsRequest request,
            ITenantConfigService config,
            HttpContext http) =>
        {
            var tenantId = http.User.GetTenantId();
            if (tenantId is null) return Results.Forbid();

            if (request.OvertimeMultiplier < 1m || request.OvertimeMultiplier > 10m)
                return Results.BadRequest(new { message = "Mnożnik nadgodzin musi mieścić się w 1.0 – 10.0" });
            if (request.NightMultiplier < 1m || request.NightMultiplier > 10m)
                return Results.BadRequest(new { message = "Mnożnik nocny musi mieścić się w 1.0 – 10.0" });
            if (request.HolidayMultiplier < 1m || request.HolidayMultiplier > 10m)
                return Results.BadRequest(new { message = "Mnożnik świąteczny musi mieścić się w 1.0 – 10.0" });

            await config.SetAsync(tenantId.Value, OvertimeMultiplierKey,
                request.OvertimeMultiplier.ToString(CultureInfo.InvariantCulture));
            await config.SetAsync(tenantId.Value, NightMultiplierKey,
                request.NightMultiplier.ToString(CultureInfo.InvariantCulture));
            if (!TimeOnly.TryParse(request.NocOd, CultureInfo.InvariantCulture, out var nocOd)
                || !TimeOnly.TryParse(request.NocDo, CultureInfo.InvariantCulture, out var nocDo))
            {
                return Results.BadRequest(new { message = "Porę nocną podaj w formacie GG:MM." });
            }

            if (nocOd == nocDo)
                return Results.BadRequest(new { message = "Początek i koniec pory nocnej nie mogą być takie same." });

            await config.SetAsync(tenantId.Value, HolidayMultiplierKey,
                request.HolidayMultiplier.ToString(CultureInfo.InvariantCulture));
            await config.SetAsync(tenantId.Value, NocOdKey, nocOd.ToString("HH:mm"));
            await config.SetAsync(tenantId.Value, NocDoKey, nocDo.ToString("HH:mm"));

            return Results.NoContent();
        })
        .WithName("UpdatePayrollSettings")
        .WithSummary("Zapisz ustawienia naliczania wynagrodzeń (mnożniki)")
        .RequirePermission("config.manage");

        return endpoints;
    }

    /// <summary>Godzina zapisana jako GG:MM. Zla wartosc cofa sie do domyslnej zamiast wywracac ekran.</summary>
    internal static TimeOnly ParseGodzine(string? value, TimeOnly fallback)
        => TimeOnly.TryParse(value, CultureInfo.InvariantCulture, out var v) ? v : fallback;

    /// <summary>Pora nocna firmy — czytana takze przez rozliczenie.</summary>
    public static async Task<(TimeOnly Od, TimeOnly Do)> PobierzPoreNocnaAsync(
        ITenantConfigService config, Guid tenantId, CancellationToken ct = default)
    {
        var od = await config.GetAsync(tenantId, NocOdKey, ct);
        var do_ = await config.GetAsync(tenantId, NocDoKey, ct);
        return (ParseGodzine(od, DomyslnaNocOd), ParseGodzine(do_, DomyslnaNocDo));
    }

    /// <summary>Mnozniki firmy — czytane takze przez rozliczenie.</summary>
    public static async Task<(decimal Nadgodziny, decimal Nocny, decimal Swiateczny)> PobierzMnoznikiAsync(
        ITenantConfigService config, Guid tenantId, CancellationToken ct = default)
    {
        return (
            Parse(await config.GetAsync(tenantId, OvertimeMultiplierKey, ct), 1.5m),
            Parse(await config.GetAsync(tenantId, NightMultiplierKey, ct), 1.2m),
            Parse(await config.GetAsync(tenantId, HolidayMultiplierKey, ct), 2.0m));
    }

    private static decimal Parse(string? value, decimal fallback)
    {
        if (string.IsNullOrWhiteSpace(value)) return fallback;
        return decimal.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out var v) ? v : fallback;
    }
}

public sealed record PayrollSettingsDto(
    decimal OvertimeMultiplier,
    decimal NightMultiplier,
    decimal HolidayMultiplier,
    string NocOd,
    string NocDo);

public sealed record UpdatePayrollSettingsRequest(
    decimal OvertimeMultiplier,
    decimal NightMultiplier,
    decimal HolidayMultiplier,
    string NocOd,
    string NocDo);

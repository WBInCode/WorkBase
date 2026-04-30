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

            return Results.Ok(new PayrollSettingsDto(
                Parse(ot, 1.5m),
                Parse(night, 1.2m),
                Parse(holiday, 2.0m)));
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
            await config.SetAsync(tenantId.Value, HolidayMultiplierKey,
                request.HolidayMultiplier.ToString(CultureInfo.InvariantCulture));

            return Results.NoContent();
        })
        .WithName("UpdatePayrollSettings")
        .WithSummary("Zapisz ustawienia naliczania wynagrodzeń (mnożniki)")
        .RequirePermission("config.manage");

        return endpoints;
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
    decimal HolidayMultiplier);

public sealed record UpdatePayrollSettingsRequest(
    decimal OvertimeMultiplier,
    decimal NightMultiplier,
    decimal HolidayMultiplier);

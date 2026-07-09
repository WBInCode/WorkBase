using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using WorkBase.Modules.TimeTracking.Application.Contracts;
using WorkBase.Shared.Auth;
using WorkBase.Shared.Domain;

namespace WorkBase.Host.Endpoints;

/// <summary>
/// Tenant-configurable anomaly detection thresholds/toggles for TimeTracking
/// (docs/AUDIT-KNOWLEDGE-MAP.md — module parametrization). Stored via
/// <see cref="ITenantConfigService"/> under the same "anomaly_detection" key that
/// <c>DbAnomalySettingsProvider</c> already reads, so saving here immediately takes effect
/// for the EndOfDayAnomalyCheckJob without any other wiring.
/// </summary>
public static class TimeTrackingSettingsEndpoints
{
    private const string ConfigKey = "anomaly_detection";

    public static IEndpointRouteBuilder MapTimeTrackingSettingsEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/time-tracking/settings").WithTags("TimeTrackingSettings").RequireAuthorization();

        group.MapGet("/", async (ITenantConfigService config, HttpContext http) =>
        {
            var tenantId = http.User.GetTenantId();
            if (tenantId is null) return Results.Forbid();

            var settings = await config.GetAsync<AnomalyDetectionSettings>(tenantId.Value, ConfigKey)
                ?? new AnomalyDetectionSettings();
            return Results.Ok(settings);
        }).WithName("GetTimeTrackingSettings").WithSummary("Pobierz ustawienia wykrywania anomalii czasu pracy");

        group.MapPut("/", async (AnomalyDetectionSettings request, ITenantConfigService config, HttpContext http) =>
        {
            var tenantId = http.User.GetTenantId();
            if (tenantId is null) return Results.Forbid();

            if (request.LateArrivalThreshold < TimeSpan.Zero || request.LateArrivalThreshold > TimeSpan.FromHours(4))
                return Results.BadRequest(new { message = "Próg spóźnienia musi mieścić się w 0–4h." });
            if (request.ExcessiveShiftThreshold < TimeSpan.FromHours(4) || request.ExcessiveShiftThreshold > TimeSpan.FromHours(24))
                return Results.BadRequest(new { message = "Próg zbyt długiej zmiany musi mieścić się w 4–24h." });

            await config.SetAsync(tenantId.Value, ConfigKey, request);
            return Results.NoContent();
        }).WithName("UpdateTimeTrackingSettings").WithSummary("Zapisz ustawienia wykrywania anomalii czasu pracy").RequirePermission("config.manage");

        return endpoints;
    }
}

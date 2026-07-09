using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using WorkBase.Modules.Tasks.Application.Contracts;
using WorkBase.Shared.Auth;
using WorkBase.Shared.Domain;

namespace WorkBase.Host.Endpoints;

/// <summary>
/// Tenant-configurable overdue-task detection behavior (docs/AUDIT-KNOWLEDGE-MAP.md — module
/// parametrization). Stored via <see cref="ITenantConfigService"/> under the "task_overdue"
/// key — the same key <c>TaskOverdueDetectorJob</c> already reads.
/// </summary>
public static class TaskSettingsEndpoints
{
    private const string ConfigKey = "task_overdue";

    public static IEndpointRouteBuilder MapTaskSettingsEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/tasks/settings").WithTags("TaskSettings").RequireAuthorization();

        group.MapGet("/", async (ITenantConfigService config, HttpContext http) =>
        {
            var tenantId = http.User.GetTenantId();
            if (tenantId is null) return Results.Forbid();

            var settings = await config.GetAsync<TaskOverdueSettings>(tenantId.Value, ConfigKey)
                ?? new TaskOverdueSettings();
            return Results.Ok(settings);
        }).WithName("GetTaskSettings").WithSummary("Pobierz ustawienia wykrywania zaległych zadań");

        group.MapPut("/", async (TaskOverdueSettings request, ITenantConfigService config, HttpContext http) =>
        {
            var tenantId = http.User.GetTenantId();
            if (tenantId is null) return Results.Forbid();

            if (request.GracePeriodHours < 0 || request.GracePeriodHours > 720)
                return Results.BadRequest(new { message = "Okres karencji musi mieścić się między 0 a 720 godzin (30 dni)." });

            await config.SetAsync(tenantId.Value, ConfigKey, request);
            return Results.NoContent();
        }).WithName("UpdateTaskSettings").WithSummary("Zapisz ustawienia wykrywania zaległych zadań").RequirePermission("config.manage");

        return endpoints;
    }
}

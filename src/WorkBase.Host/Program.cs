using Hangfire;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Scalar.AspNetCore;
using Serilog;
using WorkBase.Infrastructure;
using WorkBase.Infrastructure.BackgroundJobs;
using WorkBase.Infrastructure.Seeding;
using WorkBase.Modules.Identity.Api.Endpoints;
using WorkBase.Modules.Organization.Api.Endpoints;
using WorkBase.Modules.Organization.Infrastructure;
using WorkBase.Modules.TimeTracking.Api.Endpoints;
using WorkBase.Modules.TimeTracking.Infrastructure;
using WorkBase.Modules.TimeTracking.Infrastructure.Jobs;
using WorkBase.Modules.Workflow.Api.Endpoints;
using WorkBase.Modules.Workflow.Infrastructure;
using WorkBase.Modules.Leave.Api.Endpoints;
using WorkBase.Modules.Leave.Infrastructure;
using WorkBase.Modules.Tasks.Api.Endpoints;
using WorkBase.Modules.Tasks.Infrastructure;
using WorkBase.Modules.Tasks.Infrastructure.Jobs;
using WorkBase.Modules.Dashboard.Api.Endpoints;
using WorkBase.Modules.Dashboard.Infrastructure;
using WorkBase.Modules.Notification.Api.Endpoints;
using WorkBase.Modules.Notification.Infrastructure;
using WorkBase.Modules.Notification.Infrastructure.Hubs;
using WorkBase.Modules.Documents.Api.Endpoints;
using WorkBase.Modules.Documents.Infrastructure;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    builder.Host.UseSerilog((context, services, configuration) => configuration
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services));

    builder.Services.AddOpenApi(options =>
    {
        options.AddDocumentTransformer((document, _, _) =>
        {
            document.Info.Title = "WorkBase API";
            document.Info.Version = "v1";
            document.Info.Description = "WorkBase — B2B SaaS operational management platform";

            document.Components ??= new();
            document.Components.SecuritySchemes["Bearer"] = new()
            {
                Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT",
                Description = "Keycloak JWT token. Use: Bearer {token}"
            };

            document.SecurityRequirements.Add(new()
            {
                [new() { Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http, Scheme = "bearer", Reference = new() { Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme, Id = "Bearer" } }] = Array.Empty<string>()
            });

            return Task.CompletedTask;
        });
    });

    builder.Services.AddWorkBaseInfrastructure(builder.Configuration);

    builder.Services.AddOrganizationModule();
    builder.Services.AddTimeTrackingModule();
    builder.Services.AddWorkflowModule();
    builder.Services.AddLeaveModule();
    builder.Services.AddTasksModule();
    builder.Services.AddDashboardModule();
    builder.Services.AddNotificationModule();
    builder.Services.AddDocumentsModule();
    builder.Services.AddScoped<WorkBase.Modules.Identity.Application.Contracts.IDataScopeManagementService, WorkBase.Modules.Identity.Infrastructure.Services.DataScopeManagementService>();
    builder.Services.AddScoped<WorkBase.Modules.Identity.Application.Contracts.IFeatureFlagService, WorkBase.Modules.Identity.Infrastructure.Services.FeatureFlagService>();
    builder.Services.AddSignalR();

    var allowedOrigins = builder.Configuration
        .GetSection("Cors:AllowedOrigins")
        .Get<string[]>() ?? [];

    builder.Services.AddCors(options =>
    {
        options.AddDefaultPolicy(policy =>
        {
            policy.WithOrigins(allowedOrigins)
                  .AllowAnyHeader()
                  .AllowAnyMethod()
                  .AllowCredentials();
        });
    });

    var app = builder.Build();

    app.MapOpenApi();

    if (app.Environment.IsDevelopment())
    {
        app.MapScalarApiReference(options =>
        {
            options.Title = "WorkBase API";
            options.Theme = ScalarTheme.BluePlanet;
        });
    }

    app.UseCors();

    app.UseExceptionHandler();

    app.UseAuthentication();
    app.UseAuthorization();

    app.UseSerilogRequestLogging(options =>
    {
        options.EnrichDiagnosticContext = (diagnosticContext, httpContext) =>
        {
            diagnosticContext.Set("RequestHost", httpContext.Request.Host.Value ?? string.Empty);
            diagnosticContext.Set("UserAgent", httpContext.Request.Headers.UserAgent.ToString());
        };
    });

    app.MapHangfireDashboard("/hangfire", new DashboardOptions
    {
        DashboardTitle = "WorkBase Jobs",
        // TODO: Replace with proper auth filter after T-E02 (JWT + RBAC)
        Authorization = [new HangfireLocalRequestOnlyFilter()]
    });

    app.MapGet("/", () => Results.Ok(new { Service = "WorkBase API", Status = "Running" }));

    app.MapAuthEndpoints();
    app.MapRoleEndpoints();
    app.MapPermissionEndpoints();
    app.MapUserRoleEndpoints();
    app.MapDataScopeEndpoints();
    app.MapFeatureFlagEndpoints();

    app.MapOrganizationUnitEndpoints();
    app.MapEmployeeEndpoints();
    app.MapPositionEndpoints();
    app.MapUnitTypeEndpoints();

    app.MapTimeEntryEndpoints();
    app.MapQrTokenEndpoints();
    app.MapScheduleEndpoints();
    app.MapAnomalyEndpoints();
    app.MapTimeCorrectionEndpoints();

    app.MapWorkflowEndpoints();

    app.MapLeaveEndpoints();

    app.MapTaskEndpoints();

    app.MapDashboardEndpoints();

    app.MapNotificationEndpoints();
    app.MapHub<NotificationHub>("/hubs/notifications");

    app.MapDocumentEndpoints();

    RecurringJob.AddOrUpdate<EndOfDayAnomalyCheckJob>(
        "anomaly-detection-daily",
        job => job.ExecuteAsync(),
        "0 1 * * *",
        new RecurringJobOptions { TimeZone = TimeZoneInfo.Utc });

    RecurringJob.AddOrUpdate<TaskOverdueDetectorJob>(
        "task-overdue-detection-daily",
        job => job.ExecuteAsync(),
        "0 6 * * *",
        new RecurringJobOptions { TimeZone = TimeZoneInfo.Utc });

    app.MapHealthChecks("/health", new HealthCheckOptions
    {
        ResponseWriter = async (context, report) =>
        {
            context.Response.ContentType = "application/json";
            var result = new
            {
                status = report.Status.ToString(),
                checks = report.Entries.Select(e => new
                {
                    name = e.Key,
                    status = e.Value.Status.ToString(),
                    description = e.Value.Description,
                    duration = e.Value.Duration.TotalMilliseconds
                }),
                totalDuration = report.TotalDuration.TotalMilliseconds
            };
            await context.Response.WriteAsJsonAsync(result);
        }
    });

    app.MapHealthChecks("/health/ready", new HealthCheckOptions
    {
        Predicate = check => check.Tags.Contains("ready")
    });

    await DatabaseSeeder.SeedAsync(app.Services);

    app.Run();
}
catch (Exception ex) when (ex is not HostAbortedException)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}

namespace WorkBase.Host
{
    public partial class Program;
}

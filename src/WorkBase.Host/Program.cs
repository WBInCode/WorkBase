using Hangfire;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Scalar.AspNetCore;
using Serilog;
using WorkBase.Host.Endpoints;
using WorkBase.Infrastructure;
using WorkBase.Infrastructure.BackgroundJobs;
using WorkBase.Infrastructure.Seeding;
using WorkBase.Modules.Notification.Infrastructure.Hubs;
using WorkBase.Modules.TimeTracking.Infrastructure.Jobs;
using WorkBase.Modules.Tasks.Infrastructure.Jobs;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    Dapper.DefaultTypeMap.MatchNamesWithUnderscores = true;
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

    // Auto-discover and register all IModule implementations
    builder.Services.AddModules();
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

    app.UseRateLimiter();

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
        Authorization = [new HangfireAdminAuthorizationFilter()]
    });

    app.MapGet("/", () => Results.Ok(new { Service = "WorkBase API", Status = "Running" }));

    // Auto-discover and map all IEndpointModule implementations
    app.MapModuleEndpoints();
    app.MapWorkspaceEndpoints();
    app.MapCardSectionEndpoints();
    app.MapSavedViewEndpoints();
    app.MapActivityFeedEndpoints();
    app.MapDepartmentModuleEndpoints();
    app.MapBrandingEndpoints();
    app.MapOnboardingEndpoints();
    app.MapBillingEndpoints();
    app.MapSyncEndpoints();
    app.MapPayrollSettingsEndpoints();
    app.MapTerminologyEndpoints();
    app.MapTimeTrackingSettingsEndpoints();
    app.MapDocumentSettingsEndpoints();
    app.MapTaskSettingsEndpoints();
    app.MapHub<NotificationHub>("/hubs/notifications");

    if (!app.Environment.IsEnvironment("Testing"))
    {
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

        RecurringJob.AddOrUpdate<OrgUnitScheduleRollingGenerationJob>(
            "org-unit-schedule-rolling-generation",
            job => job.ExecuteAsync(),
            "0 2 * * 1", // Every Monday at 02:00 UTC
            new RecurringJobOptions { TimeZone = TimeZoneInfo.Utc });
    }

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

    if (!app.Environment.IsEnvironment("Testing"))
    {
        await DatabaseSeeder.SeedAsync(app.Services);
    }

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

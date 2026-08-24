using Hangfire;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.HttpOverrides;
using System.Net;
using Scalar.AspNetCore;
using Serilog;
using WorkBase.Host.Endpoints;
using WorkBase.Infrastructure;
using WorkBase.Infrastructure.BackgroundJobs;
using WorkBase.Infrastructure.Seeding;
using WorkBase.Modules.Organization.Application.Commands.Positions;
using WorkBase.Shared.Domain;
using WorkBase.Modules.Notification.Infrastructure.Hubs;
using WorkBase.Modules.TimeTracking.Infrastructure.Jobs;
using WorkBase.Modules.Tasks.Infrastructure.Jobs;
using WorkBase.Infrastructure.Ecosystem;

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

    builder.Services.Configure<ForwardedHeadersOptions>(options =>
    {
        options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;

        // Domyslna lista zawiera tylko loopback, a Traefik stoi pod adresem z sieci Dockera,
        // wiec bez tego naglowki bylyby ignorowane. Dopuszczamy wylacznie zakresy prywatne —
        // ufanie dowolnemu nadawcy pozwoliloby podac obcy adres w X-Forwarded-For.
        options.KnownNetworks.Clear();
        options.KnownProxies.Clear();
        options.KnownNetworks.Add(new Microsoft.AspNetCore.HttpOverrides.IPNetwork(IPAddress.Parse("10.0.0.0"), 8));
        options.KnownNetworks.Add(new Microsoft.AspNetCore.HttpOverrides.IPNetwork(IPAddress.Parse("172.16.0.0"), 12));
        options.KnownNetworks.Add(new Microsoft.AspNetCore.HttpOverrides.IPNetwork(IPAddress.Parse("192.168.0.0"), 16));
        options.KnownNetworks.Add(new Microsoft.AspNetCore.HttpOverrides.IPNetwork(IPAddress.Parse("127.0.0.0"), 8));

        // Miedzy klientem a aplikacja stoi dokladnie jeden posrednik (Traefik). Wieksza
        // wartosc pozwolilaby doklejac wlasne wpisy do naglowka i falszowac adres zrodlowy.
        options.ForwardLimit = 1;
    });

    var app = builder.Build();

    if (args.Contains("--migrate-only", StringComparer.OrdinalIgnoreCase))
    {
        await DatabaseSeeder.MigrateAsync(app.Services);
        return;
    }

    // Jednorazowe nadrobienie polityki stanowisk dla instalacji, ktore skonfigurowaly
    // stanowiska, zanim ta polityka powstala. To samo robi endpoint /api/org/positions/reapply-policy,
    // ale tu nie trzeba konta administratora — przydaje sie przy naprawie dzialajacej instalacji.
    // Uzycie: --reapply-position-policy <id-firmy>
    if (args.Contains("--reapply-position-policy", StringComparer.OrdinalIgnoreCase))
    {
        var indeks = Array.FindIndex(args, a => a.Equals("--reapply-position-policy", StringComparison.OrdinalIgnoreCase));
        if (indeks + 1 >= args.Length || !Guid.TryParse(args[indeks + 1], out var firmaId))
        {
            Console.Error.WriteLine("Podaj identyfikator firmy: --reapply-position-policy <guid>");
            Environment.ExitCode = 1;
            return;
        }

        using var zakres = app.Services.CreateScope();
        // Z pominieciem potoku MediatR: TenantBehavior czyta firme z tokenu HTTP, ktorego
        // w wierszu polecen nie ma. Handler zapisuje zmiany sam, wiec nie traci nic z potoku.
        var handler = zakres.ServiceProvider
            .GetRequiredService<MediatR.IRequestHandler<ReapplyPositionPolicyCommand, Result<ReapplyPositionPolicyResult>>>();
        var wynik = await handler.Handle(new ReapplyPositionPolicyCommand { TenantId = firmaId }, CancellationToken.None);
        Console.WriteLine(wynik.IsSuccess
            ? $"Przetworzono przypisan: {wynik.Value.PrzetworzonychPrzypisan}, pominieto: {wynik.Value.Pominietych}"
            : $"Nie powiodlo sie: {wynik.Error}");
        Environment.ExitCode = wynik.IsSuccess ? 0 : 1;
        return;
    }

    // Dane pokazowe dla srodowiska demonstracyjnego: struktura dzialow, pracownicy,
    // stawki, grafiki i czas pracy. Celowo poza rozruchem aplikacji — uruchamiane recznie.
    // Uzycie: --seed-demo <id-firmy>
    if (args.Contains("--seed-demo", StringComparer.OrdinalIgnoreCase))
    {
        var indeks = Array.FindIndex(args, a => a.Equals("--seed-demo", StringComparison.OrdinalIgnoreCase));
        if (indeks + 1 >= args.Length || !Guid.TryParse(args[indeks + 1], out var firmaId))
        {
            Console.Error.WriteLine("Podaj identyfikator firmy: --seed-demo <guid>");
            Environment.ExitCode = 1;
            return;
        }

        using var zakres = app.Services.CreateScope();
        var db = zakres.ServiceProvider.GetRequiredService<WorkBase.Infrastructure.Persistence.WorkBaseDbContext>();
        var dziennik = zakres.ServiceProvider
            .GetRequiredService<ILoggerFactory>().CreateLogger("SeedDemo");
        try
        {
            await WorkBase.Infrastructure.Seeding.DemoDataSeeder.SeedAsync(db, firmaId, dziennik);
        }
        catch (Exception blad)
        {
            Console.Error.WriteLine($"Nie powiodlo sie: {blad.Message}");
            Environment.ExitCode = 1;
        }
        return;
    }

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

    // Musi isc PRZED limiterem: bez tego Connection.RemoteIpAddress to zawsze adres
    // kontenera Traefika, wiec caly ruch bez logowania (ktory nie ma tenant_id) trafia
    // do JEDNEJ partycji limitu — jeden natretny klient wyczerpywal limit wszystkim.
    // Naglowki przyjmujemy wylacznie z sieci prywatnych, bo X-Forwarded-For przyslany
    // wprost od klienta pozwolilby podszyc sie pod dowolny adres i ominac limity.
    app.UseForwardedHeaders();

    app.UseRateLimiter();

    app.UseExceptionHandler();

    app.UseAuthentication();
    app.UseAuthorization();

    // PO autoryzacji, bo firme odczytuje z roszczenia w tokenie. Dopoki nowa firma nie
    // ukonczy kreatora pierwszego startu, reszta API odpowiada 409 SETUP_REQUIRED.
    // Firmy zalozone przed powstaniem kreatora nie maja znacznika i nie sa tym objete.
    app.UseMiddleware<WorkBase.Infrastructure.Setup.KonfiguracjaStartowaMiddleware>();

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
    app.MapHubIntegrationEndpoints();
    app.MapEcosystemTaskEndpoints();
    app.MapSetupEndpoints();
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

        RecurringJob.AddOrUpdate<WorkBase.Infrastructure.HubPlatform.HubEmployeeAccessJob>(
            "hub-employee-invitations",
            job => job.ExecuteAsync(),
            "* * * * *",
            new RecurringJobOptions { TimeZone = TimeZoneInfo.Utc });

        RecurringJob.AddOrUpdate<EcosystemSyncScheduler>(
            "rytm-ecosystem-snapshot",
            job => job.EnqueueAllAsync(),
            "*/15 * * * *",
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

        // Hub ekosystemu: pierwsza synchronizacja modułów po starcie (fail-soft —
        // bez Huba WorkBase działa na lokalnych feature flags jak dotychczas).
        var hubSync = app.Services.GetRequiredService<WorkBase.Infrastructure.HubPlatform.HubEntitlementsSyncService>();
        _ = hubSync.SyncAllAsync();
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

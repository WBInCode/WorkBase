using Hangfire;
using Serilog;
using WorkBase.Infrastructure;
using WorkBase.Infrastructure.BackgroundJobs;
using WorkBase.Infrastructure.Seeding;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    builder.Host.UseSerilog((context, services, configuration) => configuration
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services));

    builder.Services.AddWorkBaseInfrastructure(builder.Configuration);

    var app = builder.Build();

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

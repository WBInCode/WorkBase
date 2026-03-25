using WorkBase.Infrastructure;
using WorkBase.Infrastructure.Seeding;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddWorkBaseInfrastructure(builder.Configuration);

var app = builder.Build();

app.MapGet("/", () => Results.Ok(new { Service = "WorkBase API", Status = "Running" }));

await DatabaseSeeder.SeedAsync(app.Services);

app.Run();

namespace WorkBase.Host
{
    public partial class Program;
}

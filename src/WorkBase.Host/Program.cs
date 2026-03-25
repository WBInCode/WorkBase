var builder = WebApplication.CreateBuilder(args);

var app = builder.Build();

app.MapGet("/", () => Results.Ok(new { Service = "WorkBase API", Status = "Running" }));

app.Run();

namespace WorkBase.Host
{
    public partial class Program;
}

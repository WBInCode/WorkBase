using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using WorkBase.Infrastructure.Persistence;

namespace WorkBase.Infrastructure.Seeding;

public static class DatabaseSeeder
{
    public static async Task SeedAsync(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<WorkBaseDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<WorkBaseDbContext>>();

        logger.LogInformation("Applying database migrations...");
        await dbContext.Database.MigrateAsync();
        logger.LogInformation("Database migrations applied successfully.");

        await IamSeeder.SeedAsync(dbContext, logger);
        await WorkflowSeeder.SeedAsync(dbContext, logger);
        await LeaveSeeder.SeedAsync(dbContext, logger);
        await TaskSeeder.SeedAsync(dbContext, logger);

        logger.LogInformation("Database seeding completed.");
    }
}

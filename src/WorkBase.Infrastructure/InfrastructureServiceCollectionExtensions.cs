using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Serilog.Core;
using WorkBase.Infrastructure.Logging;
using WorkBase.Infrastructure.Persistence;

namespace WorkBase.Infrastructure;

public static class InfrastructureServiceCollectionExtensions
{
    public static IServiceCollection AddWorkBaseInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddHttpContextAccessor();

        services.AddDbContext<WorkBaseDbContext>(options =>
        {
            options.UseNpgsql(
                configuration.GetConnectionString("DefaultConnection"),
                npgsql =>
                {
                    npgsql.MigrationsAssembly(typeof(WorkBaseDbContext).Assembly.FullName);
                    npgsql.MigrationsHistoryTable("__ef_migrations_history");
                });

            options.UseSnakeCaseNamingConvention();
        });

        services.AddSingleton<ILogEventEnricher, UserContextEnricher>();

        return services;
    }
}

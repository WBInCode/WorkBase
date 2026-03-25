using Hangfire;
using Hangfire.PostgreSql;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Minio;
using Serilog.Core;
using WorkBase.Infrastructure.Logging;
using WorkBase.Infrastructure.Persistence;
using WorkBase.Infrastructure.Storage;
using WorkBase.Shared.Storage;

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

        services.AddHangfire(config => config
            .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
            .UseSimpleAssemblyNameTypeSerializer()
            .UseRecommendedSerializerSettings()
            .UsePostgreSqlStorage(options =>
                options.UseNpgsqlConnection(
                    configuration.GetConnectionString("DefaultConnection"))));

        services.AddHangfireServer(options =>
        {
            options.Queues = ["critical", "default", "reports"];
        });

        var storageOptions = configuration
            .GetSection(StorageOptions.SectionName)
            .Get<StorageOptions>() ?? new StorageOptions();

        services.AddSingleton<IMinioClient>(_ =>
        {
            var client = new MinioClient()
                .WithEndpoint(storageOptions.Endpoint)
                .WithCredentials(storageOptions.AccessKey, storageOptions.SecretKey);

            if (storageOptions.UseSSL)
            {
                client = client.WithSSL();
            }

            return client.Build();
        });

        services.AddSingleton<IFileStorage, MinioFileStorage>();

        services.AddSingleton<ILogEventEnricher, UserContextEnricher>();

        return services;
    }
}

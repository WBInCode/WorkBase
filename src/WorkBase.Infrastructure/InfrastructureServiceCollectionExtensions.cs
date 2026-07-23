using System.Reflection;
using FluentValidation;
using Hangfire;
using Hangfire.PostgreSql;
using HealthChecks.Hangfire;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Minio;
using Serilog.Core;
using WorkBase.Contracts;
using WorkBase.Infrastructure.Auth;
using WorkBase.Infrastructure.Behaviors;
using WorkBase.Infrastructure.Email;
using WorkBase.Infrastructure.Logging;
using WorkBase.Infrastructure.Persistence;
using WorkBase.Infrastructure.Storage;
using WorkBase.Infrastructure.Middleware;
using WorkBase.Infrastructure.Notifications;
using WorkBase.Infrastructure.PublicApi;
using WorkBase.Shared.Auth;
using WorkBase.Shared.Domain;
using WorkBase.Shared.Storage;

namespace WorkBase.Infrastructure;

public static class InfrastructureServiceCollectionExtensions
{
    public static IServiceCollection AddWorkBaseInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddHttpContextAccessor();

        services.AddExceptionHandler<Middleware.GlobalExceptionHandler>();
        services.AddProblemDetails();

        services.AddWorkBaseAuthentication(configuration);

        services.AddMemoryCache();
        services.AddSingleton<TenantAccessCache>();
        services.AddScoped<IPermissionService, PermissionService>();
        services.AddScoped<IRoleManagementService, RoleManagementService>();
        services.AddScoped<IAuthorizationHandler, PermissionAuthorizationHandler>();

        services.AddScoped<UserProvisioningService>();
        services.AddHttpClient();
        services.AddScoped<IKeycloakAdminService, KeycloakAdminService>();

        // Integracja z Hubem ekosystemu (wb-platform) — opcjonalna (Hub:Enabled).
        // Singleton: bezstanowy, sam tworzy scope na DbContext przy synchronizacji.
        services.AddSingleton<HubPlatform.HubEntitlementsSyncService>();
        // Singleton: cache'uje ConfigurationManager (JWKS Huba) między żądaniami handoff.
        services.AddSingleton<HubPlatform.HubSsoService>();
        services.AddScoped<IEmployeeAccessProvisioningQueue, HubPlatform.EmployeeAccessProvisioningQueue>();
        services.AddScoped<IHubEmployeeIdentityLinker, HubPlatform.HubEmployeeIdentityLinker>();
        services.AddScoped<IEmployeeAccessStatusService, HubPlatform.EmployeeAccessStatusService>();
        services.AddScoped<HubPlatform.HubEmployeeAccessJob>();
        services.AddSingleton<HubPlatform.HubUserAccessVerifier>();

        services.AddScoped<ICurrentTenantService, HttpContextTenantService>();
        services.AddScoped<IDataScopeService, DataScopeService>();
        services.AddScoped<ITenantConfigService, Services.TenantConfigService>();
        services.AddScoped<ITenantProvisioningService, Services.TenantProvisioningService>();
        services.AddScoped<IEmailSender, SmtpEmailSender>();

        var moduleApplicationAssemblies = GetModuleApplicationAssemblies().ToArray();

        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssemblies(moduleApplicationAssemblies);
            cfg.AddOpenBehavior(typeof(LoggingBehavior<,>));
            cfg.AddOpenBehavior(typeof(ValidationBehavior<,>));
            cfg.AddOpenBehavior(typeof(TenantBehavior<,>));
            cfg.AddOpenBehavior(typeof(UnitOfWorkBehavior<,>));
        });

        services.AddValidatorsFromAssemblies(moduleApplicationAssemblies, includeInternalTypes: true);

        services.AddScoped<DomainEventInterceptor>();
        services.AddScoped<AuditSaveChangesInterceptor>();

        services.AddDbContext<WorkBaseDbContext>((sp, options) =>
        {
            options.UseNpgsql(
                configuration.GetConnectionString("DefaultConnection"),
                npgsql =>
                {
                    npgsql.MigrationsAssembly(typeof(WorkBaseDbContext).Assembly.FullName);
                    npgsql.MigrationsHistoryTable("__ef_migrations_history");
                });

            options.UseSnakeCaseNamingConvention();

            options.ConfigureWarnings(w =>
                w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.PendingModelChangesWarning));

            options.AddInterceptors(
                sp.GetRequiredService<AuditSaveChangesInterceptor>(),
                sp.GetRequiredService<DomainEventInterceptor>());
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
        services.AddTenantRateLimiting(configuration);

        var connectionString = configuration.GetConnectionString("DefaultConnection")!;
        var keycloakAuthority = configuration["Keycloak:Authority"];

        services.AddHealthChecks()
            .AddNpgSql(connectionString, name: "postgresql", tags: ["db", "ready"])
            .AddHangfire(options => options.MinimumAvailableServers = 1, name: "hangfire", tags: ["jobs", "ready"])
            .AddCheck<MinioHealthCheck>("minio", tags: ["storage", "ready"]);

        if (!string.IsNullOrEmpty(keycloakAuthority))
        {
            services.AddHealthChecks()
                .AddUrlGroup(new Uri(keycloakAuthority), name: "keycloak", tags: ["auth", "ready"]);
        }

        // Public API & Webhooks
        services.AddSingleton<IApiKeyService, ApiKeyService>();
        services.AddScoped<IWebhookSubscriptionRepository, InMemoryWebhookSubscriptionRepository>();
        services.AddScoped<IWebhookDeliveryLogRepository, InMemoryWebhookDeliveryLogRepository>();
        services.AddScoped<IWebhookDispatcher, WebhookDispatcher>();
        services.AddHttpClient("Webhook");

        // Push Notifications
        services.AddScoped<Notifications.IPushSubscriptionRepository, Notifications.InMemoryPushSubscriptionRepository>();
        services.AddScoped<IPushNotificationService, FcmPushNotificationService>();
        services.AddHttpClient("FCM");

        return services;
    }

    private static IEnumerable<Assembly> GetModuleApplicationAssemblies()
    {
        var moduleNames = new[]
        {
            "WorkBase.Modules.Identity.Application",
            "WorkBase.Modules.Organization.Application",
            "WorkBase.Modules.TimeTracking.Application",
            "WorkBase.Modules.Leave.Application",
            "WorkBase.Modules.Tasks.Application",
            "WorkBase.Modules.Workflow.Application",
            "WorkBase.Modules.Dashboard.Application",
            "WorkBase.Modules.Notification.Application",
            "WorkBase.Modules.Documents.Application",
            "WorkBase.Modules.Integration.Application"
        };

        foreach (var name in moduleNames)
        {
            Assembly? assembly = null;
            try { assembly = Assembly.Load(name); } catch { /* Module not loaded */ }
            if (assembly is not null)
                yield return assembly;
        }
    }
}

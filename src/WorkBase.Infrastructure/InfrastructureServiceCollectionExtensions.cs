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
using WorkBase.Infrastructure.PublicApi;
using WorkBase.Shared.Auth;
using WorkBase.Shared.Domain;
using WorkBase.Shared.Storage;
using WorkBase.Contracts.Ecosystem;
using WorkBase.Infrastructure.Ecosystem;

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
        services.AddScoped<IAuthorizationCacheInvalidator, AuthorizationCacheInvalidator>();
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
        services.AddScoped<Terminy.TerminyPrzypomnieniaJob>();
        services.AddScoped<Workflow.EskalacjeObiegowJob>();
        services.AddSingleton<HubPlatform.HubUserAccessVerifier>();
        services.AddScoped<IHubNotificationForwarder, HubPlatform.HubNotificationForwarder>();
        services.AddScoped<HubPlatform.HubNotificationJob>();

        services.AddScoped<IChatNoticeForwarder, Chat.ChatNoticeForwarder>();
        services.AddScoped<Chat.ChatNoticeJob>();
        // Krótki limit czasu: powiadomienie do czatu jest dodatkiem, więc nie może
        // blokować wątku roboczego kolejki, gdy czat nie odpowiada.
        services.AddHttpClient("chat-notices", client =>
        {
            client.Timeout = TimeSpan.FromSeconds(8);
        });

        services.AddOptions<EcosystemOptions>()
            .Bind(configuration.GetSection(EcosystemOptions.SectionName))
            .Validate(options => !options.Enabled || (
                options.TenantId != Guid.Empty
                && Uri.TryCreate(options.BaseUrl, UriKind.Absolute, out _)
                && !string.IsNullOrWhiteSpace(options.Secret)
                && !string.IsNullOrWhiteSpace(options.HubOrgId)),
                "Ecosystem requires TenantId, BaseUrl, Secret and HubOrgId when enabled")
            .ValidateOnStart();
        services.AddScoped<EcosystemSnapshotJob>();
        services.AddScoped<EcosystemSyncScheduler>();
        services.AddScoped<IEcosystemSyncScheduler>(provider => provider.GetRequiredService<EcosystemSyncScheduler>());
        services.AddHttpClient("RytmEcosystem", (provider, client) =>
        {
            var options = provider.GetRequiredService<Microsoft.Extensions.Options.IOptions<EcosystemOptions>>().Value;
            client.BaseAddress = new Uri(options.BaseUrl.TrimEnd('/') + "/");
            client.Timeout = TimeSpan.FromSeconds(8);
        });

        services.AddScoped<ICurrentTenantService, HttpContextTenantService>();        services.AddScoped<IDataScopeService, DataScopeService>();
        services.AddScoped<IEmployeeScopeResolver, EmployeeScopeResolver>();
        services.AddScoped<ITenantConfigService, Services.TenantConfigService>();
        services.AddScoped<Setup.IKonfiguracjaStartowaService, Setup.KonfiguracjaStartowaService>();
        services.AddScoped<ITenantProvisioningService, Services.TenantProvisioningService>();
        services.AddScoped<IKioskAccountProvisioningService, Services.KioskAccountProvisioningService>();
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

        // Handlery spinające dwa moduły mieszkają w Infrastructure, którego MediatR nie skanuje
        // (skanuje tylko zestawy *.Application), więc rejestrujemy je wprost.
        services.AddScoped<MediatR.INotificationHandler<Modules.Workflow.Domain.Events.WorkflowInstanceCompletedEvent>,
            Leave.ZamknijWniosekUrlopowyPoObiegu>();
        services.AddScoped<MediatR.INotificationHandler<Modules.Workflow.Domain.Events.WorkflowInstanceRejectedEvent>,
            Leave.ZamknijWniosekUrlopowyPoObiegu>();

        services.AddScoped<MediatR.INotificationHandler<Modules.Workflow.Domain.Events.WorkflowInstanceCompletedEvent>,
            Wnioski.ZamknijWniosekPoObiegu>();
        services.AddScoped<MediatR.INotificationHandler<Modules.Workflow.Domain.Events.WorkflowInstanceRejectedEvent>,
            Wnioski.ZamknijWniosekPoObiegu>();

        // TaskOverdueDetectorJob publikowal to zdarzenie codziennie od poczatku i NIKT go nie
        // obslugiwal — zadanie pracowalo w prozni.
        services.AddScoped<MediatR.INotificationHandler<Modules.Tasks.Domain.Events.TaskOverdueEvent>,
            Tasks.PowiadomOZaleglymZadaniu>();

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

            // PendingModelChangesWarning zostaje WLACZONY (domyslne zachowanie EF 9: wyjatek przy
            // Migrate). To jedyny mechanizm, ktory wylapuje encje dodane do modelu bez migracji.
            // Byl tu wyciszony i przez to 17 tabel szesciu modulow nigdy nie trafilo do bazy,
            // a bledna wartosc domyslna time_schedules.source kasowala grafiki indywidualne.
            // Jesli start aplikacji padnie z tym ostrzezeniem — wygeneruj migracje, nie wyciszaj.

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

        services.AddOptions<Security.ClamAvOptions>()
            .Bind(configuration.GetSection(Security.ClamAvOptions.SectionName))
            .Validate(options => !options.Enabled || (!string.IsNullOrWhiteSpace(options.Host) && options.Port > 0),
                "ClamAv wymaga Host i Port, gdy jest wlaczony")
            .ValidateOnStart();
        services.AddSingleton<IMalwareScanner, Security.ClamAvScanner>();
        services.AddSingleton<WorkBase.Shared.Security.IUploadScanGuard, Security.UploadScanGuard>();

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

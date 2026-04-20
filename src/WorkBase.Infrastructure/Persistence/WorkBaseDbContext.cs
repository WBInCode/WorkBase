using System.Linq.Expressions;
using System.Reflection;
using Microsoft.EntityFrameworkCore;
using WorkBase.Shared.Domain;

namespace WorkBase.Infrastructure.Persistence;

public class WorkBaseDbContext : DbContext
{
    private readonly ICurrentTenantService? _tenantService;

    public WorkBaseDbContext(DbContextOptions<WorkBaseDbContext> options)
        : base(options)
    {
    }

    public WorkBaseDbContext(DbContextOptions<WorkBaseDbContext> options, ICurrentTenantService tenantService)
        : base(options)
    {
        _tenantService = tenantService;
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        ApplyUuidV7Convention(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(WorkBaseDbContext).Assembly);

        foreach (var assembly in GetModuleInfrastructureAssemblies())
        {
            modelBuilder.ApplyConfigurationsFromAssembly(assembly);
        }

        ApplyTenantQueryFilters(modelBuilder);
    }

    private void ApplyTenantQueryFilters(ModelBuilder modelBuilder)
    {
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (!typeof(ITenantScoped).IsAssignableFrom(entityType.ClrType))
                continue;

            var parameter = Expression.Parameter(entityType.ClrType, "e");
            var tenantIdProperty = Expression.Property(parameter, nameof(ITenantScoped.TenantId));
            var tenantService = Expression.Constant(this);
            var currentTenantProp = Expression.Property(tenantService, nameof(CurrentTenantId));

            var tenantIdAsNullable = Expression.Convert(tenantIdProperty, typeof(Guid?));

            var filter = Expression.Lambda(
                Expression.OrElse(
                    Expression.Equal(currentTenantProp, Expression.Constant(null, typeof(Guid?))),
                    Expression.Equal(tenantIdAsNullable, currentTenantProp)),
                parameter);

            entityType.SetQueryFilter(filter);
        }
    }

    public Guid? CurrentTenantId => _tenantService?.TenantId;

    private static void ApplyUuidV7Convention(ModelBuilder modelBuilder)
    {
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            var idProperty = entityType.FindProperty("Id");
            if (idProperty is not null && idProperty.ClrType == typeof(Guid))
            {
                idProperty.SetValueGeneratorFactory((_, _) => new UuidV7ValueGenerator());
                idProperty.SetDefaultValueSql(null);
            }
        }
    }

    private static IEnumerable<Assembly> GetModuleInfrastructureAssemblies()
    {
        var moduleNames = new[]
        {
            "WorkBase.Modules.Identity.Infrastructure",
            "WorkBase.Modules.Organization.Infrastructure",
            "WorkBase.Modules.TimeTracking.Infrastructure",
            "WorkBase.Modules.Leave.Infrastructure",
            "WorkBase.Modules.Tasks.Infrastructure",
            "WorkBase.Modules.Workflow.Infrastructure",
            "WorkBase.Modules.Dashboard.Infrastructure",
            "WorkBase.Modules.Notification.Infrastructure",
            "WorkBase.Modules.Documents.Infrastructure"
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

using System.Linq.Expressions;
using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using WorkBase.Shared.Domain;
using WorkBase.Shared.Modules;

namespace WorkBase.Infrastructure.Persistence;

public class WorkBaseDbContext : DbContext
{
    private readonly ICurrentTenantService? _tenantService;

    public WorkBaseDbContext(
        DbContextOptions<WorkBaseDbContext> options,
        ICurrentTenantService? tenantService = null)
        : base(options)
    {
        _tenantService = tenantService;
    }

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        configurationBuilder.Properties<DateTime>()
            .HaveConversion<UtcDateTimeConverter>();
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        foreach (var entry in ChangeTracker.Entries<AuditableEntity<Guid>>())
        {
            if (entry.State == EntityState.Added)
            {
                entry.Entity.SetCreated(DateTime.UtcNow);
            }
            else if (entry.State == EntityState.Modified)
            {
                entry.Entity.SetModified(DateTime.UtcNow);
            }
        }

        return await base.SaveChangesAsync(cancellationToken);
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
        // Sourced from the single ModuleCatalog (src/WorkBase.Shared/Modules/ModuleCatalog.cs).
        // Previously this list only covered 9/15 modules, meaning EF Core never applied entity
        // configurations for Integration/Cases/Contacts/Forms/Sales/AI — their tables were never
        // part of the model at all, regardless of migrations or feature flags.
        var moduleNames = ModuleCatalog.All
            .Select(m => $"WorkBase.Modules.{m.Namespace}.Infrastructure");

        foreach (var name in moduleNames)
        {
            Assembly? assembly = null;
            try { assembly = Assembly.Load(name); } catch { /* Module not loaded */ }
            if (assembly is not null)
                yield return assembly;
        }
    }
}

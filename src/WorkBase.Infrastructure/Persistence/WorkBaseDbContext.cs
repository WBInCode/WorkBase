using System.Reflection;
using Microsoft.EntityFrameworkCore;

namespace WorkBase.Infrastructure.Persistence;

public class WorkBaseDbContext : DbContext
{
    public WorkBaseDbContext(DbContextOptions<WorkBaseDbContext> options)
        : base(options)
    {
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
    }

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

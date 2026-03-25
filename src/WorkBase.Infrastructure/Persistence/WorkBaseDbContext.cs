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
}

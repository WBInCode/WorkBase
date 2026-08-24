using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using WorkBase.Infrastructure.Persistence;
using WorkBase.Modules.Leave.Domain.Entities;

namespace WorkBase.Infrastructure.Seeding;

public static class LeaveSeeder
{
    private static readonly Guid DefaultTenantId = Guid.Parse("00000000-0000-0000-0000-000000000001");

    /// <summary>
    /// Typy urlopow dla wskazanej firmy. Dopisuje tylko brakujace kody, wiec mozna wolac
    /// wielokrotnie — i tak sie dzieje, bo provisioning powtarza sie przy kazdej synchronizacji
    /// z Hubem.
    /// </summary>
    /// <remarks>
    /// <see cref="SeedAsync"/> obsluguje wylacznie firme operatora: ma zaszyty
    /// <see cref="DefaultTenantId"/> i przerywa, gdy w bazie istnieje JAKIKOLWIEK typ urlopu.
    /// Od pierwszego uruchomienia aplikacji kazda kolejna firma dostawala wiec pusta liste
    /// i jej pracownicy nie mieli czego wybrac we wniosku urlopowym.
    /// </remarks>
    public static async Task SeedTenantAsync(
        WorkBaseDbContext dbContext,
        Guid tenantId,
        ILogger logger,
        CancellationToken cancellationToken = default)
    {
        // IgnoreQueryFilters, bo to leci poza zadaniem HTTP — globalny filtr najemcy
        // nie ma wtedy z czego odczytac firmy i odsialby wszystko.
        var istniejace = await dbContext.Set<LeaveType>()
            .IgnoreQueryFilters()
            .Where(typ => typ.TenantId == tenantId)
            .Select(typ => typ.Code)
            .ToListAsync(cancellationToken);

        var brakujace = Domyslne(tenantId)
            .Where(typ => !istniejace.Contains(typ.Code))
            .ToList();

        if (brakujace.Count == 0) return;

        dbContext.Set<LeaveType>().AddRange(brakujace);
        await dbContext.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Dopisano {Count} typow urlopu dla firmy {TenantId}.", brakujace.Count, tenantId);
    }

    public static async Task SeedAsync(WorkBaseDbContext dbContext, ILogger logger)
    {
        if (await dbContext.Set<LeaveType>().AnyAsync())
        {
            logger.LogInformation("Leave types already seeded, skipping.");
            return;
        }

        logger.LogInformation("Seeding leave types...");

        var leaveTypes = Domyslne(DefaultTenantId);

        dbContext.Set<LeaveType>().AddRange(leaveTypes);
        await dbContext.SaveChangesAsync();

        logger.LogInformation("Leave seeding completed: {Count} leave types.", leaveTypes.Count);
    }

    /// <summary>Zestaw zgodny z Kodeksem pracy — ten sam dla kazdej firmy na start.</summary>
    private static List<LeaveType> Domyslne(Guid tenantId)
    {
        return new List<LeaveType>
        {
            LeaveType.Create(tenantId, "ANNUAL", "Urlop wypoczynkowy",
                isPaid: true, requiresApproval: true, defaultDaysPerYear: 26,
                description: "Roczny urlop wypoczynkowy", color: "#4CAF50", sortOrder: 1),

            LeaveType.Create(tenantId, "ON_DEMAND", "Urlop na żądanie",
                isPaid: true, requiresApproval: false, defaultDaysPerYear: 4,
                description: "Urlop na żądanie (wliczany w pulę urlopu wypoczynkowego)", color: "#FF9800", sortOrder: 2),

            LeaveType.Create(tenantId, "SICK", "Zwolnienie lekarskie (L4)",
                isPaid: true, requiresApproval: false, defaultDaysPerYear: null,
                description: "Zwolnienie lekarskie — bez limitu dni, wymagane zaświadczenie", color: "#F44336", sortOrder: 3),

            LeaveType.Create(tenantId, "CHILDCARE", "Opieka nad dzieckiem",
                isPaid: true, requiresApproval: true, defaultDaysPerYear: 2,
                description: "Opieka nad dzieckiem do lat 14 (art. 188 KP)", color: "#9C27B0", sortOrder: 4),
        };
    }
}

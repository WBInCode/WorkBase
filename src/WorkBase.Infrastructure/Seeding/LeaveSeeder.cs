using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using WorkBase.Infrastructure.Persistence;
using WorkBase.Modules.Leave.Domain.Entities;

namespace WorkBase.Infrastructure.Seeding;

public static class LeaveSeeder
{
    private static readonly Guid DefaultTenantId = Guid.Parse("00000000-0000-0000-0000-000000000001");

    public static async Task SeedAsync(WorkBaseDbContext dbContext, ILogger logger)
    {
        if (await dbContext.Set<LeaveType>().AnyAsync())
        {
            logger.LogInformation("Leave types already seeded, skipping.");
            return;
        }

        logger.LogInformation("Seeding leave types...");

        var leaveTypes = new List<LeaveType>
        {
            LeaveType.Create(DefaultTenantId, "ANNUAL", "Urlop wypoczynkowy",
                isPaid: true, requiresApproval: true, defaultDaysPerYear: 26,
                description: "Roczny urlop wypoczynkowy", color: "#4CAF50", sortOrder: 1),

            LeaveType.Create(DefaultTenantId, "ON_DEMAND", "Urlop na żądanie",
                isPaid: true, requiresApproval: false, defaultDaysPerYear: 4,
                description: "Urlop na żądanie (wliczany w pulę urlopu wypoczynkowego)", color: "#FF9800", sortOrder: 2),

            LeaveType.Create(DefaultTenantId, "SICK", "Zwolnienie lekarskie (L4)",
                isPaid: true, requiresApproval: false, defaultDaysPerYear: null,
                description: "Zwolnienie lekarskie — bez limitu dni, wymagane zaświadczenie", color: "#F44336", sortOrder: 3),

            LeaveType.Create(DefaultTenantId, "CHILDCARE", "Opieka nad dzieckiem",
                isPaid: true, requiresApproval: true, defaultDaysPerYear: 2,
                description: "Opieka nad dzieckiem do lat 14 (art. 188 KP)", color: "#9C27B0", sortOrder: 4),
        };

        dbContext.Set<LeaveType>().AddRange(leaveTypes);
        await dbContext.SaveChangesAsync();

        logger.LogInformation("Leave seeding completed: {Count} leave types.", leaveTypes.Count);
    }
}

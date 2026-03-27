using Microsoft.Extensions.DependencyInjection;
using WorkBase.Modules.TimeTracking.Application.Contracts;
using WorkBase.Modules.TimeTracking.Infrastructure.Repositories;

namespace WorkBase.Modules.TimeTracking.Infrastructure;

public static class TimeTrackingServiceCollectionExtensions
{
    public static IServiceCollection AddTimeTrackingModule(this IServiceCollection services)
    {
        services.AddScoped<ITimeEntryRepository, TimeEntryRepository>();
        services.AddScoped<ITimeSheetRepository, TimeSheetRepository>();

        return services;
    }
}

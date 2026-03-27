using Microsoft.Extensions.DependencyInjection;
using WorkBase.Modules.TimeTracking.Application.Contracts;
using WorkBase.Modules.TimeTracking.Application.Services;
using WorkBase.Modules.TimeTracking.Infrastructure.Jobs;
using WorkBase.Modules.TimeTracking.Infrastructure.Repositories;

namespace WorkBase.Modules.TimeTracking.Infrastructure;

public static class TimeTrackingServiceCollectionExtensions
{
    public static IServiceCollection AddTimeTrackingModule(this IServiceCollection services)
    {
        services.AddScoped<ITimeEntryRepository, TimeEntryRepository>();
        services.AddScoped<ITimeSheetRepository, TimeSheetRepository>();
        services.AddScoped<IQrTokenRepository, QrTokenRepository>();
        services.AddScoped<IScheduleRepository, ScheduleRepository>();
        services.AddScoped<IScheduleTemplateRepository, ScheduleTemplateRepository>();
        services.AddScoped<ITimeAnomalyRepository, TimeAnomalyRepository>();

        services.AddScoped<AnomalyDetectionService>();
        services.AddScoped<EndOfDayAnomalyCheckJob>();

        return services;
    }
}

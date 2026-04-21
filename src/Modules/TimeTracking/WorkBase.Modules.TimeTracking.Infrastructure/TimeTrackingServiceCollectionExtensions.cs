using Microsoft.Extensions.DependencyInjection;
using WorkBase.Modules.TimeTracking.Application.Contracts;
using WorkBase.Modules.TimeTracking.Application.Rules;
using WorkBase.Modules.TimeTracking.Application.Services;
using WorkBase.Modules.TimeTracking.Infrastructure.Jobs;
using WorkBase.Modules.TimeTracking.Infrastructure.Repositories;
using WorkBase.Shared.Domain;

namespace WorkBase.Modules.TimeTracking.Infrastructure;

public sealed class TimeTrackingModule : IModule
{
    public IServiceCollection ConfigureServices(IServiceCollection services) =>
        services.AddTimeTrackingModule();
}

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
        services.AddScoped<ITimeCorrectionRepository, TimeCorrectionRepository>();
        services.AddScoped<INfcBadgeRepository, NfcBadgeRepository>();
        services.AddScoped<IBiometricTemplateRepository, BiometricTemplateRepository>();
        services.AddScoped<IGeofenceZoneRepository, GeofenceZoneRepository>();
        services.AddScoped<IGeofenceEventRepository, GeofenceEventRepository>();

        // Anomaly rules (Strategy pattern — each rule is an IAnomalyRule)
        services.AddScoped<IAnomalyRule, MissingClockOutRule>();
        services.AddScoped<IAnomalyRule, MissingClockInRule>();
        services.AddScoped<IAnomalyRule, LateArrivalRule>();
        services.AddScoped<IAnomalyRule, DoubleClockInRule>();
        services.AddScoped<IAnomalyRule, OverlongShiftRule>();

        // Anomaly services
        services.AddScoped<IAnomalySettingsProvider, DbAnomalySettingsProvider>();
        services.AddScoped<AnomalyDetectionService>();
        services.AddScoped<EndOfDayAnomalyCheckJob>();

        return services;
    }
}

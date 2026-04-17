using Microsoft.Extensions.DependencyInjection;
using WorkBase.Modules.Leave.Application.Contracts;
using WorkBase.Modules.Leave.Application.Services;
using WorkBase.Modules.Leave.Infrastructure.Repositories;

namespace WorkBase.Modules.Leave.Infrastructure;

public static class LeaveServiceCollectionExtensions
{
    public static IServiceCollection AddLeaveModule(this IServiceCollection services)
    {
        services.AddScoped<ILeaveRequestRepository, LeaveRequestRepository>();
        services.AddScoped<ILeaveBalanceRepository, LeaveBalanceRepository>();
        services.AddScoped<ILeaveTypeRepository, LeaveTypeRepository>();
        services.AddScoped<ILeavePolicyRepository, LeavePolicyRepository>();
        services.AddScoped<ILeaveCalendarEntryRepository, LeaveCalendarEntryRepository>();

        services.AddScoped<ILeaveBalanceCalculator, LeaveBalanceCalculator>();

        return services;
    }
}

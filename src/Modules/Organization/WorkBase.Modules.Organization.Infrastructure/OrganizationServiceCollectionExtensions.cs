using Microsoft.Extensions.DependencyInjection;
using WorkBase.Modules.Organization.Application.Contracts;
using WorkBase.Modules.Organization.Infrastructure.Repositories;

namespace WorkBase.Modules.Organization.Infrastructure;

public static class OrganizationServiceCollectionExtensions
{
    public static IServiceCollection AddOrganizationModule(this IServiceCollection services)
    {
        services.AddScoped<IOrganizationUnitRepository, OrganizationUnitRepository>();
        services.AddScoped<IOrganizationUnitTypeRepository, OrganizationUnitTypeRepository>();
        services.AddScoped<IEmployeeRepository, EmployeeRepository>();
        services.AddScoped<IEmployeeAssignmentRepository, EmployeeAssignmentRepository>();
        services.AddScoped<IPositionRepository, PositionRepository>();
        services.AddScoped<ISupervisorRelationRepository, SupervisorRelationRepository>();

        return services;
    }
}

using Microsoft.Extensions.DependencyInjection;
using WorkBase.Contracts;
using WorkBase.Modules.Organization.Application.Contracts;
using WorkBase.Modules.Organization.Infrastructure.Repositories;
using WorkBase.Modules.Organization.Infrastructure.Services;
using WorkBase.Shared.Domain;

namespace WorkBase.Modules.Organization.Infrastructure;

public sealed class OrganizationModule : IModule
{
    public IServiceCollection ConfigureServices(IServiceCollection services) =>
        services.AddOrganizationModule();
}

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
        services.AddScoped<ISupervisorLookupService, SupervisorLookupService>();
        services.AddScoped<IOrganizationLookupService, OrganizationLookupService>();

        return services;
    }
}

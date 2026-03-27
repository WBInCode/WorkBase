using Microsoft.Extensions.DependencyInjection;
using WorkBase.Modules.Workflow.Application;
using WorkBase.Modules.Workflow.Application.Contracts;
using WorkBase.Modules.Workflow.Infrastructure.Repositories;

namespace WorkBase.Modules.Workflow.Infrastructure;

public static class WorkflowServiceCollectionExtensions
{
    public static IServiceCollection AddWorkflowModule(this IServiceCollection services)
    {
        services.AddScoped<IWorkflowDefinitionRepository, WorkflowDefinitionRepository>();
        services.AddScoped<IWorkflowInstanceRepository, WorkflowInstanceRepository>();
        services.AddScoped<IWorkflowStepRepository, WorkflowStepRepository>();
        services.AddScoped<IWorkflowActionRepository, WorkflowActionRepository>();

        services.AddScoped<IWorkflowEngine, WorkflowEngine>();

        return services;
    }
}

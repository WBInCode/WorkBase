using Microsoft.Extensions.DependencyInjection;
using WorkBase.Contracts;
using WorkBase.Modules.Workflow.Application;
using WorkBase.Modules.Workflow.Application.Contracts;
using WorkBase.Modules.Workflow.Application.Services;
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
        services.AddScoped<IApprovalRequestRepository, ApprovalRequestRepository>();
        services.AddScoped<IApprovalDecisionRepository, ApprovalDecisionRepository>();
        services.AddScoped<IEscalationRuleRepository, EscalationRuleRepository>();

        services.AddScoped<IWorkflowEngine, WorkflowEngine>();
        services.AddScoped<IApproverResolver, ApproverResolver>();
        services.AddScoped<IWorkflowService, WorkflowService>();
        services.AddScoped<IWorkflowActionExecutor, WorkflowActionExecutor>();

        return services;
    }
}

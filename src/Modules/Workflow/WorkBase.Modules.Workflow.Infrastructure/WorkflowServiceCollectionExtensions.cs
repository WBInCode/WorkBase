using Microsoft.Extensions.DependencyInjection;
using WorkBase.Contracts;
using WorkBase.Modules.Workflow.Application;
using WorkBase.Modules.Workflow.Application.Contracts;
using WorkBase.Modules.Workflow.Application.Services;
using WorkBase.Modules.Workflow.Infrastructure.Repositories;
using WorkBase.Shared.Domain;

namespace WorkBase.Modules.Workflow.Infrastructure;

public sealed class WorkflowModule : IModule
{
    public IServiceCollection ConfigureServices(IServiceCollection services) =>
        services.AddWorkflowModule();
}

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
        services.AddScoped<IWorkflowBranchRepository, WorkflowBranchRepository>();

        services.AddScoped<IWorkflowEngine, WorkflowEngine>();
        services.AddScoped<IApproverResolver, ApproverResolver>();
        services.AddScoped<IWorkflowService, WorkflowService>();
        services.AddScoped<IWorkflowActionExecutor, WorkflowActionExecutor>();
        services.AddSingleton<IConditionEvaluator, SimpleConditionEvaluator>();

        return services;
    }
}

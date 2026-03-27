using Microsoft.Extensions.DependencyInjection;

namespace WorkBase.Modules.Workflow.Infrastructure;

public static class WorkflowServiceCollectionExtensions
{
    public static IServiceCollection AddWorkflowModule(this IServiceCollection services)
    {
        // Repositories will be registered as the module grows (T-E09-002+)
        return services;
    }
}

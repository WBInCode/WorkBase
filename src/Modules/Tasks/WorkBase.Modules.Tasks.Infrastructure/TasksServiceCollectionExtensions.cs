using Microsoft.Extensions.DependencyInjection;
using WorkBase.Modules.Tasks.Application.Contracts;
using WorkBase.Modules.Tasks.Application.Services;
using WorkBase.Modules.Tasks.Infrastructure.Repositories;
using WorkBase.Shared.Domain;

namespace WorkBase.Modules.Tasks.Infrastructure;

public sealed class TasksModule : IModule
{
    public IServiceCollection ConfigureServices(IServiceCollection services) =>
        services.AddTasksModule();
}

public static class TasksServiceCollectionExtensions
{
    public static IServiceCollection AddTasksModule(this IServiceCollection services)
    {
        services.AddScoped<ITaskItemRepository, TaskItemRepository>();
        services.AddScoped<ITaskStatusRepository, TaskStatusRepository>();
        services.AddScoped<ITaskPriorityRepository, TaskPriorityRepository>();
        services.AddScoped<ITaskStatusTransitionRepository, TaskStatusTransitionRepository>();
        services.AddScoped<ITaskCommentRepository, TaskCommentRepository>();
        services.AddScoped<ITaskAttachmentRepository, TaskAttachmentRepository>();
        services.AddScoped<ITaskHistoryRepository, TaskHistoryRepository>();

        services.AddScoped<ITaskStatusMachine, TaskStatusMachine>();

        return services;
    }
}

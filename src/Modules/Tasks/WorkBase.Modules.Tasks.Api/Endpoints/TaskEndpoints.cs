using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using WorkBase.Modules.Tasks.Application.Commands;
using WorkBase.Modules.Tasks.Application.Dtos;
using WorkBase.Modules.Tasks.Application.Queries;
using WorkBase.Shared.Api;
using WorkBase.Shared.Auth;

namespace WorkBase.Modules.Tasks.Api.Endpoints;

public static class TaskEndpoints
{
    public static IEndpointRouteBuilder MapTaskEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/tasks")
            .WithTags("Tasks")
            .RequireAuthorization();

        // --- Statuses ---
        group.MapGet("/statuses", GetStatuses)
            .WithName("GetTaskStatuses")
            .WithSummary("Pobierz statusy zadań")
            .RequirePermission("tasks.view")
            .Produces<List<TaskStatusDto>>();

        // --- Priorities ---
        group.MapGet("/priorities", GetPriorities)
            .WithName("GetTaskPriorities")
            .WithSummary("Pobierz priorytety zadań")
            .RequirePermission("tasks.view")
            .Produces<List<TaskPriorityDto>>();

        // --- Tasks CRUD ---
        group.MapGet("/", GetTasks)
            .WithName("GetTasks")
            .WithSummary("Pobierz listę zadań")
            .RequirePermission("tasks.view")
            .Produces<List<TaskItemDto>>();

        group.MapGet("/{id:guid}", GetTaskById)
            .WithName("GetTaskById")
            .WithSummary("Pobierz szczegóły zadania")
            .RequirePermission("tasks.view")
            .Produces<TaskItemDto>()
            .Produces(StatusCodes.Status404NotFound);

        group.MapPost("/", CreateTask)
            .WithName("CreateTask")
            .WithSummary("Utwórz nowe zadanie")
            .RequirePermission("tasks.create")
            .Produces<Guid>(StatusCodes.Status201Created);

        group.MapPut("/{id:guid}", UpdateTask)
            .WithName("UpdateTask")
            .WithSummary("Zaktualizuj zadanie")
            .RequirePermission("tasks.edit")
            .Produces(StatusCodes.Status200OK);

        group.MapPut("/{id:guid}/status", ChangeStatus)
            .WithName("ChangeTaskStatus")
            .WithSummary("Zmień status zadania")
            .RequirePermission("tasks.edit")
            .Produces(StatusCodes.Status200OK);

        group.MapPut("/{id:guid}/assign", AssignTask)
            .WithName("AssignTask")
            .WithSummary("Przypisz zadanie do pracownika")
            .RequirePermission("tasks.edit")
            .Produces(StatusCodes.Status200OK);

        // --- Comments ---
        group.MapPost("/{id:guid}/comments", AddComment)
            .WithName("AddTaskComment")
            .WithSummary("Dodaj komentarz do zadania")
            .RequirePermission("tasks.edit")
            .Produces<Guid>(StatusCodes.Status201Created);

        // --- Attachments ---
        group.MapPost("/{id:guid}/attachments", AddAttachment)
            .WithName("AddTaskAttachment")
            .WithSummary("Dodaj załącznik do zadania")
            .RequirePermission("tasks.edit")
            .Produces<Guid>(StatusCodes.Status201Created);

        group.MapDelete("/{id:guid}", DeleteTask)
            .WithName("DeleteTask")
            .WithSummary("Usuń zadanie")
            .RequirePermission("tasks.edit")
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status404NotFound);

        return endpoints;
    }

    private static async Task<IResult> GetStatuses(ISender sender)
    {
        var result = await sender.Send(new GetTaskStatusesQuery());
        return result.ToHttpResult();
    }

    private static async Task<IResult> GetPriorities(ISender sender)
    {
        var result = await sender.Send(new GetTaskPrioritiesQuery());
        return result.ToHttpResult();
    }

    private static async Task<IResult> GetTasks(Guid? assigneeId, ISender sender)
    {
        var result = await sender.Send(new GetTasksQuery(assigneeId));
        return result.ToHttpResult();
    }

    private static async Task<IResult> GetTaskById(Guid id, ISender sender)
    {
        var result = await sender.Send(new GetTaskByIdQuery(id));
        return result.ToHttpResult();
    }

    private static async Task<IResult> CreateTask(CreateTaskBody body, ISender sender)
    {
        var result = await sender.Send(new CreateTaskCommand(
            body.Title, body.Description, body.PriorityId,
            body.AssigneeId, body.ReporterId, body.DueDate));
        return result.IsSuccess
            ? Results.Created($"/api/tasks/{result.Value}", result.Value)
            : result.ToHttpResult();
    }

    private static async Task<IResult> UpdateTask(Guid id, UpdateTaskBody body, ISender sender)
    {
        var result = await sender.Send(new UpdateTaskCommand(
            id, body.Title, body.Description, body.PriorityId, body.DueDate));
        return result.ToHttpResult();
    }

    private static async Task<IResult> ChangeStatus(Guid id, ChangeStatusBody body, ISender sender)
    {
        var result = await sender.Send(new ChangeTaskStatusCommand(
            id, body.NewStatusId, body.ChangedById));
        return result.ToHttpResult();
    }

    private static async Task<IResult> AssignTask(Guid id, AssignTaskBody body, ISender sender)
    {
        var result = await sender.Send(new AssignTaskCommand(id, body.NewAssigneeId));
        return result.ToHttpResult();
    }

    private static async Task<IResult> AddComment(Guid id, AddCommentBody body, ISender sender)
    {
        var result = await sender.Send(new AddTaskCommentCommand(id, body.AuthorId, body.Content));
        return result.IsSuccess
            ? Results.Created($"/api/tasks/{id}/comments/{result.Value}", result.Value)
            : result.ToHttpResult();
    }

    private static async Task<IResult> AddAttachment(Guid id, AddAttachmentBody body, ISender sender)
    {
        var result = await sender.Send(new AddTaskAttachmentCommand(
            id, body.FileName, body.StoragePath, body.ContentType,
            body.FileSizeBytes, body.UploadedById));
        return result.IsSuccess
            ? Results.Created($"/api/tasks/{id}/attachments/{result.Value}", result.Value)
            : result.ToHttpResult();
    }

    private static async Task<IResult> DeleteTask(Guid id, ISender sender)
    {
        var result = await sender.Send(new DeleteTaskCommand(id));
        return result.IsSuccess ? Results.NoContent() : result.ToHttpResult();
    }
}

public sealed record CreateTaskBody(
    string Title, string? Description, Guid PriorityId,
    Guid AssigneeId, Guid? ReporterId = null, DateTime? DueDate = null);

public sealed record UpdateTaskBody(
    string Title, string? Description, Guid PriorityId, DateTime? DueDate);

public sealed record ChangeStatusBody(Guid NewStatusId, Guid ChangedById);

public sealed record AssignTaskBody(Guid NewAssigneeId);

public sealed record AddCommentBody(Guid AuthorId, string Content);

public sealed record AddAttachmentBody(
    string FileName, string StoragePath, string ContentType,
    long FileSizeBytes, Guid UploadedById);

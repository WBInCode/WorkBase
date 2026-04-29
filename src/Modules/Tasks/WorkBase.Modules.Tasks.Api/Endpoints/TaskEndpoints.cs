using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using WorkBase.Modules.Tasks.Application.Commands;
using WorkBase.Modules.Tasks.Application.Dtos;
using WorkBase.Modules.Tasks.Application.Queries;
using WorkBase.Shared.Api;
using WorkBase.Shared.Auth;
using WorkBase.Shared.Storage;

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
            .WithName("GetTaskStatuses").WithSummary("Pobierz statusy zadań")
            .RequirePermission("tasks.view").Produces<List<TaskStatusDto>>();

        group.MapPost("/statuses", CreateStatus)
            .WithName("CreateTaskStatus").WithSummary("Utwórz status zadania")
            .RequirePermission("tasks.manage").Produces<Guid>(StatusCodes.Status201Created);

        group.MapPut("/statuses/{id:guid}", UpdateStatus)
            .WithName("UpdateTaskStatus").WithSummary("Zaktualizuj status zadania")
            .RequirePermission("tasks.manage").Produces(StatusCodes.Status200OK);

        group.MapDelete("/statuses/{id:guid}", DeleteStatus)
            .WithName("DeleteTaskStatus").WithSummary("Usuń status zadania")
            .RequirePermission("tasks.manage").Produces(StatusCodes.Status204NoContent);

        group.MapGet("/statuses/transitions", GetStatusTransitions)
            .WithName("GetTaskStatusTransitions").WithSummary("Pobierz dozwolone przejścia statusów")
            .RequirePermission("tasks.view").Produces<List<TaskStatusTransitionDto>>();

        // --- Priorities ---
        group.MapGet("/priorities", GetPriorities)
            .WithName("GetTaskPriorities").WithSummary("Pobierz priorytety zadań")
            .RequirePermission("tasks.view").Produces<List<TaskPriorityDto>>();

        group.MapPost("/priorities", CreatePriority)
            .WithName("CreateTaskPriority").WithSummary("Utwórz priorytet zadania")
            .RequirePermission("tasks.manage").Produces<Guid>(StatusCodes.Status201Created);

        group.MapPut("/priorities/{id:guid}", UpdatePriority)
            .WithName("UpdateTaskPriority").WithSummary("Zaktualizuj priorytet zadania")
            .RequirePermission("tasks.manage").Produces(StatusCodes.Status200OK);

        group.MapDelete("/priorities/{id:guid}", DeletePriority)
            .WithName("DeleteTaskPriority").WithSummary("Usuń priorytet zadania")
            .RequirePermission("tasks.manage").Produces(StatusCodes.Status204NoContent);

        // --- Tasks CRUD ---
        group.MapGet("/my", GetMyTasks)
            .WithName("GetMyTasks").WithSummary("Pobierz moje zadania")
            .RequirePermission("tasks.view").Produces<List<TaskItemDto>>();

        group.MapGet("/", GetTasks)
            .WithName("GetTasks").WithSummary("Pobierz listę zadań")
            .RequirePermission("tasks.view").Produces<List<TaskItemDto>>();

        group.MapGet("/{id:guid}", GetTaskById)
            .WithName("GetTaskById").WithSummary("Pobierz szczegóły zadania")
            .RequirePermission("tasks.view").Produces<TaskItemDto>().Produces(StatusCodes.Status404NotFound);

        group.MapPost("/", CreateTask)
            .WithName("CreateTask").WithSummary("Utwórz nowe zadanie")
            .RequirePermission("tasks.create").Produces<Guid>(StatusCodes.Status201Created);

        group.MapPut("/{id:guid}", UpdateTask)
            .WithName("UpdateTask").WithSummary("Zaktualizuj zadanie")
            .RequirePermission("tasks.edit").Produces(StatusCodes.Status200OK);

        group.MapPut("/{id:guid}/status", ChangeStatus)
            .WithName("ChangeTaskStatus").WithSummary("Zmień status zadania")
            .RequirePermission("tasks.edit").Produces(StatusCodes.Status200OK);

        group.MapPut("/{id:guid}/assign", AssignTask)
            .WithName("AssignTask").WithSummary("Przypisz zadanie do pracownika")
            .RequirePermission("tasks.edit").Produces(StatusCodes.Status200OK);

        group.MapDelete("/{id:guid}", DeleteTask)
            .WithName("DeleteTask").WithSummary("Usuń zadanie")
            .RequirePermission("tasks.edit").Produces(StatusCodes.Status204NoContent).Produces(StatusCodes.Status404NotFound);

        // --- Comments ---
        group.MapGet("/{id:guid}/comments", GetComments)
            .WithName("GetTaskComments").WithSummary("Pobierz komentarze zadania")
            .RequirePermission("tasks.view").Produces<List<TaskCommentDto>>();

        group.MapPost("/{id:guid}/comments", AddComment)
            .WithName("AddTaskComment").WithSummary("Dodaj komentarz do zadania")
            .RequirePermission("tasks.edit").Produces<Guid>(StatusCodes.Status201Created);

        group.MapPut("/{taskId:guid}/comments/{commentId:guid}", UpdateComment)
            .WithName("UpdateTaskComment").WithSummary("Zaktualizuj komentarz")
            .RequirePermission("tasks.edit").Produces(StatusCodes.Status200OK);

        group.MapDelete("/{taskId:guid}/comments/{commentId:guid}", DeleteComment)
            .WithName("DeleteTaskComment").WithSummary("Usuń komentarz")
            .RequirePermission("tasks.edit").Produces(StatusCodes.Status204NoContent);

        // --- Attachments ---
        group.MapGet("/{id:guid}/attachments", GetAttachments)
            .WithName("GetTaskAttachments").WithSummary("Pobierz załączniki zadania")
            .RequirePermission("tasks.view").Produces<List<TaskAttachmentDto>>();

        group.MapPost("/{id:guid}/attachments", AddAttachment)
            .WithName("AddTaskAttachment").WithSummary("Dodaj załącznik do zadania")
            .RequirePermission("tasks.edit").Produces<Guid>(StatusCodes.Status201Created);

        group.MapGet("/{taskId:guid}/attachments/{attachmentId:guid}/download", DownloadAttachment)
            .WithName("DownloadTaskAttachment").WithSummary("Pobierz plik załącznika")
            .RequirePermission("tasks.view").Produces(StatusCodes.Status200OK);

        return endpoints;
    }

    // --- Statuses ---
    private static async Task<IResult> GetStatuses(ISender sender)
    {
        var result = await sender.Send(new GetTaskStatusesQuery());
        return result.ToHttpResult();
    }

    private static async Task<IResult> CreateStatus(CreateStatusBody body, ISender sender)
    {
        var result = await sender.Send(new CreateTaskStatusCommand(
            body.Code, body.Name, body.Color, body.IsFinal, body.IsDefault, body.SortOrder));
        return result.IsSuccess
            ? Results.Created($"/api/tasks/statuses/{result.Value}", result.Value)
            : result.ToHttpResult();
    }

    private static async Task<IResult> UpdateStatus(Guid id, UpdateStatusBody body, ISender sender)
    {
        var result = await sender.Send(new UpdateTaskStatusCommand(
            id, body.Name, body.Color, body.IsFinal, body.SortOrder));
        return result.ToHttpResult();
    }

    private static async Task<IResult> DeleteStatus(Guid id, ISender sender)
    {
        var result = await sender.Send(new DeleteTaskStatusCommand(id));
        return result.IsSuccess ? Results.NoContent() : result.ToHttpResult();
    }

    private static async Task<IResult> GetStatusTransitions(ISender sender)
    {
        var result = await sender.Send(new GetTaskStatusTransitionsQuery());
        return result.ToHttpResult();
    }

    // --- Priorities ---
    private static async Task<IResult> GetPriorities(ISender sender)
    {
        var result = await sender.Send(new GetTaskPrioritiesQuery());
        return result.ToHttpResult();
    }

    private static async Task<IResult> CreatePriority(CreatePriorityBody body, ISender sender)
    {
        var result = await sender.Send(new CreateTaskPriorityCommand(
            body.Code, body.Name, body.Color, body.SortOrder));
        return result.IsSuccess
            ? Results.Created($"/api/tasks/priorities/{result.Value}", result.Value)
            : result.ToHttpResult();
    }

    private static async Task<IResult> UpdatePriority(Guid id, UpdatePriorityBody body, ISender sender)
    {
        var result = await sender.Send(new UpdateTaskPriorityCommand(
            id, body.Name, body.Color, body.SortOrder));
        return result.ToHttpResult();
    }

    private static async Task<IResult> DeletePriority(Guid id, ISender sender)
    {
        var result = await sender.Send(new DeleteTaskPriorityCommand(id));
        return result.IsSuccess ? Results.NoContent() : result.ToHttpResult();
    }

    // --- Tasks ---
    private static async Task<IResult> GetMyTasks(ClaimsPrincipal user, ISender sender)
    {
        var employeeIdClaim = user.FindFirstValue("employee_id");
        if (!Guid.TryParse(employeeIdClaim, out var employeeId))
            return Results.Unauthorized();

        var result = await sender.Send(new GetTasksQuery(employeeId));
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
        var dueDate = body.DueDate.HasValue
            ? DateTime.SpecifyKind(body.DueDate.Value, DateTimeKind.Utc)
            : (DateTime?)null;
        var result = await sender.Send(new CreateTaskCommand(
            body.Title, body.Description, body.PriorityId,
            body.AssigneeId, body.ReporterId, dueDate, body.AdditionalAssigneeIds));
        return result.IsSuccess
            ? Results.Created($"/api/tasks/{result.Value}", result.Value)
            : result.ToHttpResult();
    }

    private static async Task<IResult> UpdateTask(Guid id, UpdateTaskBody body, ISender sender)
    {
        var dueDate = body.DueDate.HasValue
            ? DateTime.SpecifyKind(body.DueDate.Value, DateTimeKind.Utc)
            : (DateTime?)null;
        var result = await sender.Send(new UpdateTaskCommand(
            id, body.Title, body.Description, body.PriorityId, dueDate));
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
        var result = await sender.Send(new AssignTaskCommand(id, body.NewAssigneeId, body.AdditionalAssigneeIds));
        return result.ToHttpResult();
    }

    private static async Task<IResult> DeleteTask(Guid id, ISender sender)
    {
        var result = await sender.Send(new DeleteTaskCommand(id));
        return result.IsSuccess ? Results.NoContent() : result.ToHttpResult();
    }

    // --- Comments ---
    private static async Task<IResult> GetComments(Guid id, ISender sender)
    {
        var result = await sender.Send(new GetTaskCommentsByTaskQuery(id));
        return result.ToHttpResult();
    }

    private static async Task<IResult> AddComment(Guid id, AddCommentBody body, ISender sender)
    {
        var result = await sender.Send(new AddTaskCommentCommand(id, body.AuthorId, body.Content));
        return result.IsSuccess
            ? Results.Created($"/api/tasks/{id}/comments/{result.Value}", result.Value)
            : result.ToHttpResult();
    }

    private static async Task<IResult> UpdateComment(Guid taskId, Guid commentId, UpdateCommentBody body, ISender sender)
    {
        var result = await sender.Send(new UpdateTaskCommentCommand(taskId, commentId, body.Content));
        return result.ToHttpResult();
    }

    private static async Task<IResult> DeleteComment(Guid taskId, Guid commentId, ISender sender)
    {
        var result = await sender.Send(new DeleteTaskCommentCommand(taskId, commentId));
        return result.IsSuccess ? Results.NoContent() : result.ToHttpResult();
    }

    // --- Attachments ---
    private static async Task<IResult> GetAttachments(Guid id, ISender sender)
    {
        var result = await sender.Send(new GetTaskAttachmentsByTaskQuery(id));
        return result.ToHttpResult();
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

    private static async Task<IResult> DownloadAttachment(
        Guid taskId, Guid attachmentId,
        IFileStorage fileStorage, ISender sender)
    {
        var result = await sender.Send(new GetTaskAttachmentsByTaskQuery(taskId));
        if (!result.IsSuccess)
            return result.ToHttpResult();

        var attachment = result.Value.FirstOrDefault(a => a.Id == attachmentId);
        if (attachment is null)
            return Results.NotFound();

        var stream = await fileStorage.DownloadAsync("workbase", $"tasks/{taskId}/{attachment.FileName}");
        return Results.File(stream, attachment.ContentType, attachment.FileName);
    }
}

public sealed record CreateTaskBody(
    string Title, string? Description, Guid PriorityId,
    Guid AssigneeId, Guid? ReporterId = null, DateTime? DueDate = null,
    IReadOnlyList<Guid>? AdditionalAssigneeIds = null);

public sealed record UpdateTaskBody(
    string Title, string? Description, Guid PriorityId, DateTime? DueDate);

public sealed record ChangeStatusBody(Guid NewStatusId, Guid ChangedById);

public sealed record AssignTaskBody(Guid NewAssigneeId, IReadOnlyList<Guid>? AdditionalAssigneeIds = null);

public sealed record AddCommentBody(Guid AuthorId, string Content);

public sealed record UpdateCommentBody(string Content);

public sealed record AddAttachmentBody(
    string FileName, string StoragePath, string ContentType,
    long FileSizeBytes, Guid UploadedById);

public sealed record CreateStatusBody(
    string Code, string Name, string? Color,
    bool IsFinal, bool IsDefault, int SortOrder);

public sealed record UpdateStatusBody(
    string Name, string? Color, bool IsFinal, int SortOrder);

public sealed record CreatePriorityBody(
    string Code, string Name, string? Color, int SortOrder);

public sealed record UpdatePriorityBody(
    string Name, string? Color, int SortOrder);

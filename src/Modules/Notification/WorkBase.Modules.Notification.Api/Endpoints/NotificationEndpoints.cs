using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using WorkBase.Modules.Notification.Application.Commands;
using WorkBase.Modules.Notification.Application.Queries;
using WorkBase.Shared.Api;
using WorkBase.Shared.Auth;

namespace WorkBase.Modules.Notification.Api.Endpoints;

public static class NotificationEndpoints
{
    public static IEndpointRouteBuilder MapNotificationEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/notifications")
            .WithTags("Notifications")
            .RequireAuthorization();

        group.MapGet("/", GetNotifications)
            .WithName("GetNotifications")
            .WithSummary("Get notifications for current user")
            .Produces<List<NotificationDto>>();

        group.MapGet("/unread-count", GetUnreadCount)
            .WithName("GetUnreadCount")
            .WithSummary("Get unread notification count")
            .Produces<int>();

        group.MapPost("/{id:guid}/read", MarkAsRead)
            .WithName("MarkNotificationRead")
            .WithSummary("Mark a notification as read");

        group.MapPost("/mark-all-read", MarkAllRead)
            .WithName("MarkAllNotificationsRead")
            .WithSummary("Mark all notifications as read for a user");

        // --- Templates ---
        group.MapGet("/templates", GetTemplates)
            .WithName("GetNotificationTemplates")
            .WithSummary("Pobierz szablony powiadomień")
            .Produces<List<NotificationTemplateDto>>();

        group.MapPost("/templates", CreateTemplate)
            .WithName("CreateNotificationTemplate")
            .WithSummary("Utwórz szablon powiadomienia")
            .Produces<Guid>(StatusCodes.Status201Created);

        group.MapPut("/templates/{id:guid}", UpdateTemplate)
            .WithName("UpdateNotificationTemplate")
            .WithSummary("Zaktualizuj szablon powiadomienia");

        group.MapDelete("/templates/{id:guid}", DeleteTemplate)
            .WithName("DeleteNotificationTemplate")
            .WithSummary("Usuń szablon powiadomienia")
            .Produces(StatusCodes.Status204NoContent);

        // --- Preferences ---
        group.MapGet("/preferences", GetPreferences)
            .WithName("GetNotificationPreferences")
            .WithSummary("Pobierz preferencje powiadomień użytkownika")
            .Produces<List<NotificationPreferenceDto>>();

        group.MapPut("/preferences", UpdatePreference)
            .WithName("UpdateNotificationPreference")
            .WithSummary("Zaktualizuj preferencje powiadomień");

        return endpoints;
    }

    private static async Task<IResult> GetNotifications(
        Guid recipientUserId, bool? unreadOnly, int? limit, ISender sender)
    {
        var query = new GetNotificationsQuery(recipientUserId, unreadOnly ?? false, limit ?? 50);
        var result = await sender.Send(query);
        return result.ToHttpResult();
    }

    private static async Task<IResult> GetUnreadCount(Guid recipientUserId, ISender sender)
    {
        var query = new GetUnreadCountQuery(recipientUserId);
        var result = await sender.Send(query);
        return result.ToHttpResult();
    }

    private static async Task<IResult> MarkAsRead(Guid id, ISender sender)
    {
        var command = new MarkNotificationReadCommand(id);
        var result = await sender.Send(command);
        return result.ToHttpResult();
    }

    private static async Task<IResult> MarkAllRead(Guid recipientUserId, ISender sender)
    {
        var command = new MarkAllNotificationsReadCommand(recipientUserId);
        var result = await sender.Send(command);
        return result.ToHttpResult();
    }

    // --- Templates ---
    private static async Task<IResult> GetTemplates(ISender sender)
    {
        var result = await sender.Send(new GetNotificationTemplatesQuery());
        return result.ToHttpResult();
    }

    private static async Task<IResult> CreateTemplate(CreateTemplateBody body, ISender sender)
    {
        var result = await sender.Send(new CreateNotificationTemplateCommand(
            body.Code, body.Name, body.TitleTemplate, body.BodyTemplate, body.Category));
        return result.IsSuccess
            ? Results.Created($"/api/notifications/templates/{result.Value}", result.Value)
            : result.ToHttpResult();
    }

    private static async Task<IResult> UpdateTemplate(Guid id, UpdateTemplateBody body, ISender sender)
    {
        var result = await sender.Send(new UpdateNotificationTemplateCommand(
            id, body.Name, body.TitleTemplate, body.BodyTemplate, body.Category));
        return result.ToHttpResult();
    }

    private static async Task<IResult> DeleteTemplate(Guid id, ISender sender)
    {
        var result = await sender.Send(new DeleteNotificationTemplateCommand(id));
        return result.IsSuccess ? Results.NoContent() : result.ToHttpResult();
    }

    // --- Preferences ---
    private static async Task<IResult> GetPreferences(Guid userId, ISender sender)
    {
        var result = await sender.Send(new GetNotificationPreferencesQuery(userId));
        return result.ToHttpResult();
    }

    private static async Task<IResult> UpdatePreference(UpdatePreferenceBody body, ISender sender)
    {
        var result = await sender.Send(new UpdateNotificationPreferenceCommand(
            body.UserId, body.Category, body.InApp, body.Email));
        return result.ToHttpResult();
    }
}

public sealed record CreateTemplateBody(
    string Code, string Name, string TitleTemplate,
    string BodyTemplate, string Category);

public sealed record UpdateTemplateBody(
    string Name, string TitleTemplate,
    string BodyTemplate, string Category);

public sealed record UpdatePreferenceBody(
    Guid UserId, string Category, bool InApp, bool Email);

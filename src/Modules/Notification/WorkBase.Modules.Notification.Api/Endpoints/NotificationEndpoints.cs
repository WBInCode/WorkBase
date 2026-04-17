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
}

using Microsoft.AspNetCore.SignalR;
using WorkBase.Contracts;
using WorkBase.Infrastructure.Persistence;
using WorkBase.Modules.Notification.Infrastructure.Hubs;

namespace WorkBase.Modules.Notification.Infrastructure.Services;

public sealed class NotificationService(WorkBaseDbContext db, IHubContext<NotificationHub> hubContext)
    : INotificationService
{
    public async Task SendAsync(Guid tenantId, Guid recipientUserId, string title, string body,
        string category, string? referenceType = null, Guid? referenceId = null, CancellationToken ct = default)
    {
        var notification = Domain.Entities.Notification.Create(
            tenantId, recipientUserId, title, body, category, referenceType, referenceId);

        await db.Set<Domain.Entities.Notification>().AddAsync(notification, ct);
        await db.SaveChangesAsync(ct);

        await hubContext.Clients.Group($"user_{recipientUserId}").SendAsync("ReceiveNotification", new
        {
            notification.Id,
            notification.Title,
            notification.Body,
            notification.Category,
            notification.CreatedAt,
            notification.ReferenceType,
            notification.ReferenceId
        }, ct);
    }
}

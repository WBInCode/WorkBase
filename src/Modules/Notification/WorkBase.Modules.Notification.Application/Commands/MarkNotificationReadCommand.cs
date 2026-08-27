using WorkBase.Modules.Notification.Application.Contracts;
using WorkBase.Shared.Cqrs;
using WorkBase.Shared.Domain;

namespace WorkBase.Modules.Notification.Application.Commands;

/// <summary>
/// <paramref name="RecipientUserId"/> to konto pytajacego, brane z tokenu. Bez tego sprawdzenia
/// kazdy w firmie mogl oznaczyc CUDZE powiadomienie jako przeczytane — handler weryfikowal
/// wylacznie najemce.
/// </summary>
public sealed record MarkNotificationReadCommand(Guid NotificationId, Guid RecipientUserId)
    : ICommand, ITenantRequest
{
    public Guid TenantId { get; set; }
}

public sealed class MarkNotificationReadHandler(INotificationRepository repository)
    : ICommandHandler<MarkNotificationReadCommand>
{
    public async Task<Result> Handle(MarkNotificationReadCommand request, CancellationToken cancellationToken)
    {
        var notification = await repository.GetByIdAsync(request.NotificationId, cancellationToken);
        if (notification is null)
            return Result.Failure(Error.NotFound("Notification.NotFound", "Notification not found"));

        // Cudze powiadomienie zwracamy jako nieistniejace, zeby odmowa nie potwierdzala,
        // ze taki wpis jest.
        if (notification.TenantId != request.TenantId
            || notification.RecipientUserId != request.RecipientUserId)
        {
            return Result.Failure(Error.NotFound("Notification.NotFound", "Notification not found"));
        }

        notification.MarkAsRead();
        repository.Update(notification);
        await repository.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}

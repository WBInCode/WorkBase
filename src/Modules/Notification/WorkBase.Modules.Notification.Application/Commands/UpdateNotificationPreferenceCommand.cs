using WorkBase.Modules.Notification.Application.Contracts;
using WorkBase.Modules.Notification.Domain.Entities;
using WorkBase.Shared.Cqrs;
using WorkBase.Shared.Domain;

namespace WorkBase.Modules.Notification.Application.Commands;

public sealed record UpdateNotificationPreferenceCommand(
    Guid UserId, string Category, bool InApp, bool Email) : ICommand, ITenantRequest
{
    public Guid TenantId { get; set; }
}

public sealed class UpdateNotificationPreferenceHandler(INotificationPreferenceRepository repository)
    : ICommandHandler<UpdateNotificationPreferenceCommand>
{
    public async Task<Result> Handle(UpdateNotificationPreferenceCommand request, CancellationToken cancellationToken)
    {
        var pref = await repository.GetAsync(request.TenantId, request.UserId, request.Category, cancellationToken);
        if (pref is null)
        {
            pref = NotificationPreference.Create(
                request.TenantId, request.UserId, request.Category,
                request.InApp, request.Email);
            await repository.AddAsync(pref, cancellationToken);
        }
        else
        {
            pref.Update(request.InApp, request.Email);
        }

        return Result.Success();
    }
}

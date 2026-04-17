using WorkBase.Modules.Notification.Application.Contracts;
using WorkBase.Shared.Cqrs;
using WorkBase.Shared.Domain;

namespace WorkBase.Modules.Notification.Application.Commands;

public sealed record DeleteNotificationTemplateCommand(Guid Id) : ICommand, ITenantRequest
{
    public Guid TenantId { get; set; }
}

public sealed class DeleteNotificationTemplateHandler(INotificationTemplateRepository repository)
    : ICommandHandler<DeleteNotificationTemplateCommand>
{
    public async Task<Result> Handle(DeleteNotificationTemplateCommand request, CancellationToken cancellationToken)
    {
        var template = await repository.GetByIdAsync(request.Id, cancellationToken);
        if (template is null)
            return Result.Failure(Error.NotFound("NotificationTemplate.NotFound",
                $"Szablon powiadomień o id '{request.Id}' nie został znaleziony."));

        repository.Remove(template);
        return Result.Success();
    }
}

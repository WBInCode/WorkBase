using WorkBase.Modules.Notification.Application.Contracts;
using WorkBase.Shared.Cqrs;
using WorkBase.Shared.Domain;

namespace WorkBase.Modules.Notification.Application.Commands;

public sealed record UpdateNotificationTemplateCommand(
    Guid Id, string Name, string TitleTemplate,
    string BodyTemplate, string Category) : ICommand, ITenantRequest
{
    public Guid TenantId { get; set; }
}

public sealed class UpdateNotificationTemplateHandler(INotificationTemplateRepository repository)
    : ICommandHandler<UpdateNotificationTemplateCommand>
{
    public async Task<Result> Handle(UpdateNotificationTemplateCommand request, CancellationToken cancellationToken)
    {
        var template = await repository.GetByIdAsync(request.Id, cancellationToken);
        if (template is null)
            return Result.Failure(Error.NotFound("NotificationTemplate.NotFound",
                $"Szablon powiadomień o id '{request.Id}' nie został znaleziony."));

        template.Update(request.Name, request.TitleTemplate, request.BodyTemplate, request.Category);
        return Result.Success();
    }
}

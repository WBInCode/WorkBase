using WorkBase.Modules.Notification.Application.Contracts;
using WorkBase.Modules.Notification.Domain.Entities;
using WorkBase.Shared.Cqrs;
using WorkBase.Shared.Domain;

namespace WorkBase.Modules.Notification.Application.Commands;

public sealed record CreateNotificationTemplateCommand(
    string Code, string Name, string TitleTemplate,
    string BodyTemplate, string Category) : ICommand<Guid>, ITenantRequest
{
    public Guid TenantId { get; set; }
}

public sealed class CreateNotificationTemplateHandler(INotificationTemplateRepository repository)
    : ICommandHandler<CreateNotificationTemplateCommand, Guid>
{
    public async Task<Result<Guid>> Handle(CreateNotificationTemplateCommand request, CancellationToken cancellationToken)
    {
        var template = NotificationTemplate.Create(
            request.TenantId, request.Code, request.Name,
            request.TitleTemplate, request.BodyTemplate, request.Category);
        await repository.AddAsync(template, cancellationToken);
        return template.Id;
    }
}

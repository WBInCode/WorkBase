using WorkBase.Modules.TimeTracking.Application.Contracts;
using WorkBase.Shared.Cqrs;
using WorkBase.Shared.Domain;

namespace WorkBase.Modules.TimeTracking.Application.Commands;

public sealed record DeleteScheduleTemplateCommand(Guid Id) : ICommand, ITenantRequest
{
    public Guid TenantId { get; set; }
}

public sealed class DeleteScheduleTemplateHandler(IScheduleTemplateRepository repository)
    : ICommandHandler<DeleteScheduleTemplateCommand>
{
    public async Task<Result> Handle(DeleteScheduleTemplateCommand request, CancellationToken cancellationToken)
    {
        var template = await repository.GetByIdAsync(request.TenantId, request.Id, cancellationToken);
        if (template is null)
            return Result.Failure(Error.NotFound("ScheduleTemplate.NotFound", "Szablon grafiku nie został znaleziony."));

        repository.Remove(template);
        return Result.Success();
    }
}

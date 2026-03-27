using WorkBase.Modules.TimeTracking.Application.Contracts;
using WorkBase.Shared.Cqrs;
using WorkBase.Shared.Domain;

namespace WorkBase.Modules.TimeTracking.Application.Commands;

public sealed class UpdateScheduleTemplateHandler(IScheduleTemplateRepository templateRepository)
    : ICommandHandler<UpdateScheduleTemplateCommand>
{
    public async Task<Result> Handle(UpdateScheduleTemplateCommand request, CancellationToken cancellationToken)
    {
        var template = await templateRepository.GetByIdAsync(
            request.TenantId, request.TemplateId, cancellationToken);

        if (template is null)
            return Result.Failure(Error.NotFound(
                "ScheduleTemplate.NotFound",
                "Szablon grafiku nie został znaleziony."));

        var nameExists = await templateRepository.NameExistsAsync(
            request.TenantId, request.Name, request.TemplateId, cancellationToken);

        if (nameExists)
            return Result.Failure(Error.Conflict(
                "ScheduleTemplate.NameExists",
                $"Szablon o nazwie '{request.Name}' już istnieje."));

        template.Update(request.Name, request.Definition, request.Description);
        templateRepository.Update(template);

        return Result.Success();
    }
}

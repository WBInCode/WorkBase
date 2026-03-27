using WorkBase.Modules.TimeTracking.Application.Contracts;
using WorkBase.Modules.TimeTracking.Domain.Entities;
using WorkBase.Shared.Cqrs;
using WorkBase.Shared.Domain;

namespace WorkBase.Modules.TimeTracking.Application.Commands;

public sealed class CreateScheduleTemplateHandler(IScheduleTemplateRepository templateRepository)
    : ICommandHandler<CreateScheduleTemplateCommand, Guid>
{
    public async Task<Result<Guid>> Handle(CreateScheduleTemplateCommand request, CancellationToken cancellationToken)
    {
        var nameExists = await templateRepository.NameExistsAsync(
            request.TenantId, request.Name, cancellationToken: cancellationToken);

        if (nameExists)
            return Result.Failure<Guid>(Error.Conflict(
                "ScheduleTemplate.NameExists",
                $"Szablon o nazwie '{request.Name}' już istnieje."));

        var template = ScheduleTemplate.Create(
            request.TenantId,
            request.Name,
            request.Definition,
            request.Description);

        await templateRepository.AddAsync(template, cancellationToken);

        return template.Id;
    }
}

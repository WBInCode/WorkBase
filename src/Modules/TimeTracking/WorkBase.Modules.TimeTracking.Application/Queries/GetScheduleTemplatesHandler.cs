using WorkBase.Modules.TimeTracking.Application.Contracts;
using WorkBase.Modules.TimeTracking.Application.Dtos;
using WorkBase.Shared.Cqrs;
using WorkBase.Shared.Domain;

namespace WorkBase.Modules.TimeTracking.Application.Queries;

public sealed class GetScheduleTemplatesHandler(IScheduleTemplateRepository templateRepository)
    : IQueryHandler<GetScheduleTemplatesQuery, IReadOnlyList<ScheduleTemplateDto>>
{
    public async Task<Result<IReadOnlyList<ScheduleTemplateDto>>> Handle(
        GetScheduleTemplatesQuery request,
        CancellationToken cancellationToken)
    {
        var templates = await templateRepository.GetAllAsync(request.TenantId, cancellationToken);

        var dtos = templates.Select(t => new ScheduleTemplateDto(
            t.Id,
            t.Name,
            t.Description,
            t.Definition,
            t.IsActive)).ToList();

        return dtos;
    }
}

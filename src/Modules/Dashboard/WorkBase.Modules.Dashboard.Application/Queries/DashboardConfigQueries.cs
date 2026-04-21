using WorkBase.Modules.Dashboard.Application.Contracts;
using WorkBase.Modules.Dashboard.Application.Dtos;
using WorkBase.Shared.Cqrs;
using WorkBase.Shared.Domain;

namespace WorkBase.Modules.Dashboard.Application.Queries;

public sealed record GetDashboardConfigsQuery(Guid UserId) : IQuery<List<DashboardConfigDto>>, ITenantRequest
{
    public Guid TenantId { get; set; }
}

public sealed class GetDashboardConfigsHandler(
    IDashboardConfigRepository configRepo,
    IDashboardWidgetRepository widgetRepo) : IQueryHandler<GetDashboardConfigsQuery, List<DashboardConfigDto>>
{
    public async Task<Result<List<DashboardConfigDto>>> Handle(GetDashboardConfigsQuery query, CancellationToken ct)
    {
        var configs = await configRepo.GetByUserAsync(query.UserId, ct);
        var result = new List<DashboardConfigDto>();

        foreach (var c in configs)
        {
            var widgets = await widgetRepo.GetByConfigAsync(c.Id, ct);
            result.Add(new DashboardConfigDto(c.Id, c.UserId, c.Name, c.IsDefault, c.CreatedAt, c.ModifiedAt,
                widgets.OrderBy(w => w.SortOrder).Select(w => new DashboardWidgetDto(
                    w.Id, w.WidgetType, w.Title, w.Column, w.Row, w.Width, w.Height,
                    w.Settings, w.IsVisible, w.SortOrder)).ToList()));
        }

        return result;
    }
}

public sealed record GetReportsQuery : IQuery<List<ReportDefinitionDto>>, ITenantRequest
{
    public Guid TenantId { get; set; }
}

public sealed class GetReportsHandler(
    IReportDefinitionRepository repo) : IQueryHandler<GetReportsQuery, List<ReportDefinitionDto>>
{
    public async Task<Result<List<ReportDefinitionDto>>> Handle(GetReportsQuery query, CancellationToken ct)
    {
        var reports = await repo.GetByTenantAsync(query.TenantId, ct);
        return reports.Select(r => new ReportDefinitionDto(
            r.Id, r.Name, r.Description, r.ReportType, r.DataSource,
            r.FiltersJson, r.ColumnsJson, r.GroupByJson,
            r.AggregationsJson, r.ChartConfigJson, r.SortJson,
            r.IsShared, r.CreatedByUserId, r.CreatedAt)).ToList();
    }
}

public sealed record GetReportByIdQuery(Guid Id) : IQuery<ReportDefinitionDto>, ITenantRequest
{
    public Guid TenantId { get; set; }
}

public sealed class GetReportByIdHandler(
    IReportDefinitionRepository repo) : IQueryHandler<GetReportByIdQuery, ReportDefinitionDto>
{
    public async Task<Result<ReportDefinitionDto>> Handle(GetReportByIdQuery query, CancellationToken ct)
    {
        var r = await repo.GetByIdAsync(query.Id, ct);
        if (r is null) return Result.Failure<ReportDefinitionDto>(Error.NotFound("Report.NotFound", "Raport nie znaleziony."));
        return new ReportDefinitionDto(r.Id, r.Name, r.Description, r.ReportType, r.DataSource,
            r.FiltersJson, r.ColumnsJson, r.GroupByJson,
            r.AggregationsJson, r.ChartConfigJson, r.SortJson,
            r.IsShared, r.CreatedByUserId, r.CreatedAt);
    }
}

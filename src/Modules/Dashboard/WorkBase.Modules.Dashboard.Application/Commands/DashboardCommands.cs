using WorkBase.Modules.Dashboard.Application.Contracts;
using WorkBase.Modules.Dashboard.Application.Dtos;
using WorkBase.Modules.Dashboard.Domain.Entities;
using WorkBase.Shared.Cqrs;
using WorkBase.Shared.Domain;

namespace WorkBase.Modules.Dashboard.Application.Commands;

// --- Dashboard Config ---
public sealed record CreateDashboardConfigCommand(
    Guid UserId, string Name, bool IsDefault,
    List<CreateWidgetRequest> Widgets) : ICommand<Guid>, ITenantRequest
{
    public Guid TenantId { get; set; }
}

public sealed record CreateWidgetRequest(
    string WidgetType, string Title, int Column, int Row,
    int Width, int Height, string? Settings, int SortOrder);

public sealed class CreateDashboardConfigHandler(
    IDashboardConfigRepository configRepo,
    IDashboardWidgetRepository widgetRepo) : ICommandHandler<CreateDashboardConfigCommand, Guid>
{
    public async Task<Result<Guid>> Handle(CreateDashboardConfigCommand cmd, CancellationToken ct)
    {
        var config = DashboardConfig.Create(cmd.TenantId, cmd.UserId, cmd.Name, cmd.IsDefault);
        await configRepo.AddAsync(config, ct);

        foreach (var w in cmd.Widgets)
        {
            var widget = DashboardWidget.Create(cmd.TenantId, config.Id,
                w.WidgetType, w.Title, w.Column, w.Row, w.Width, w.Height, w.Settings, w.SortOrder);
            await widgetRepo.AddAsync(widget, ct);
        }

        return config.Id;
    }
}

public sealed record UpdateDashboardConfigCommand(
    Guid Id, string Name, bool IsDefault,
    List<CreateWidgetRequest> Widgets) : ICommand, ITenantRequest
{
    public Guid TenantId { get; set; }
}

public sealed class UpdateDashboardConfigHandler(
    IDashboardConfigRepository configRepo,
    IDashboardWidgetRepository widgetRepo) : ICommandHandler<UpdateDashboardConfigCommand>
{
    public async Task<Result> Handle(UpdateDashboardConfigCommand cmd, CancellationToken ct)
    {
        var config = await configRepo.GetByIdAsync(cmd.Id, ct);
        if (config is null) return Result.Failure(Error.NotFound("Dashboard.NotFound", "Konfiguracja dashboardu nie znaleziona."));

        config.Update(cmd.Name, cmd.IsDefault);
        configRepo.Update(config);

        var existing = await widgetRepo.GetByConfigAsync(cmd.Id, ct);
        widgetRepo.RemoveRange(existing);

        foreach (var w in cmd.Widgets)
        {
            var widget = DashboardWidget.Create(cmd.TenantId, config.Id,
                w.WidgetType, w.Title, w.Column, w.Row, w.Width, w.Height, w.Settings, w.SortOrder);
            await widgetRepo.AddAsync(widget, ct);
        }

        return Result.Success();
    }
}

public sealed record DeleteDashboardConfigCommand(Guid Id) : ICommand, ITenantRequest
{
    public Guid TenantId { get; set; }
}

public sealed class DeleteDashboardConfigHandler(
    IDashboardConfigRepository repo) : ICommandHandler<DeleteDashboardConfigCommand>
{
    public async Task<Result> Handle(DeleteDashboardConfigCommand cmd, CancellationToken ct)
    {
        var config = await repo.GetByIdAsync(cmd.Id, ct);
        if (config is null) return Result.Failure(Error.NotFound("Dashboard.NotFound", "Konfiguracja nie znaleziona."));
        repo.Remove(config);
        return Result.Success();
    }
}

// --- Reports ---
public sealed record CreateReportCommand(
    string Name, string? Description, string ReportType, string DataSource,
    string? FiltersJson, string? ColumnsJson, string? GroupByJson,
    string? AggregationsJson, string? ChartConfigJson, string? SortJson,
    bool IsShared, Guid CreatedByUserId) : ICommand<Guid>, ITenantRequest
{
    public Guid TenantId { get; set; }
}

public sealed class CreateReportHandler(
    IReportDefinitionRepository repo) : ICommandHandler<CreateReportCommand, Guid>
{
    public async Task<Result<Guid>> Handle(CreateReportCommand cmd, CancellationToken ct)
    {
        var report = ReportDefinition.Create(
            cmd.TenantId, cmd.Name, cmd.ReportType, cmd.DataSource,
            cmd.CreatedByUserId, cmd.Description, cmd.IsShared);
        report.Update(cmd.Name, cmd.Description, cmd.ReportType, cmd.DataSource,
            cmd.FiltersJson, cmd.ColumnsJson, cmd.GroupByJson,
            cmd.AggregationsJson, cmd.ChartConfigJson, cmd.SortJson, cmd.IsShared);
        await repo.AddAsync(report, ct);
        return report.Id;
    }
}

public sealed record UpdateReportCommand(
    Guid Id, string Name, string? Description, string ReportType, string DataSource,
    string? FiltersJson, string? ColumnsJson, string? GroupByJson,
    string? AggregationsJson, string? ChartConfigJson, string? SortJson,
    bool IsShared) : ICommand, ITenantRequest
{
    public Guid TenantId { get; set; }
}

public sealed class UpdateReportHandler(
    IReportDefinitionRepository repo) : ICommandHandler<UpdateReportCommand>
{
    public async Task<Result> Handle(UpdateReportCommand cmd, CancellationToken ct)
    {
        var report = await repo.GetByIdAsync(cmd.Id, ct);
        if (report is null) return Result.Failure(Error.NotFound("Report.NotFound", "Raport nie znaleziony."));
        report.Update(cmd.Name, cmd.Description, cmd.ReportType, cmd.DataSource,
            cmd.FiltersJson, cmd.ColumnsJson, cmd.GroupByJson,
            cmd.AggregationsJson, cmd.ChartConfigJson, cmd.SortJson, cmd.IsShared);
        repo.Update(report);
        return Result.Success();
    }
}

public sealed record DeleteReportCommand(Guid Id) : ICommand, ITenantRequest
{
    public Guid TenantId { get; set; }
}

public sealed class DeleteReportHandler(
    IReportDefinitionRepository repo) : ICommandHandler<DeleteReportCommand>
{
    public async Task<Result> Handle(DeleteReportCommand cmd, CancellationToken ct)
    {
        var report = await repo.GetByIdAsync(cmd.Id, ct);
        if (report is null) return Result.Failure(Error.NotFound("Report.NotFound", "Raport nie znaleziony."));
        repo.Remove(report);
        return Result.Success();
    }
}

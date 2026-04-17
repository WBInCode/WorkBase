using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using WorkBase.Modules.Dashboard.Application.Dtos;
using WorkBase.Modules.Dashboard.Application.Queries;
using WorkBase.Shared.Api;
using WorkBase.Shared.Auth;

namespace WorkBase.Modules.Dashboard.Api.Endpoints;

public static class DashboardEndpoints
{
    public static IEndpointRouteBuilder MapDashboardEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/dashboard")
            .WithTags("Dashboard")
            .RequireAuthorization();

        group.MapGet("/summary", GetSummary)
            .WithName("GetDashboardSummary")
            .WithSummary("Pobierz podsumowanie dashboardu")
            .RequirePermission("dashboard.view")
            .Produces<DashboardSummaryDto>();

        return endpoints;
    }

    private static async Task<IResult> GetSummary(ISender sender)
    {
        var result = await sender.Send(new GetDashboardSummaryQuery());
        return result.ToHttpResult();
    }
}

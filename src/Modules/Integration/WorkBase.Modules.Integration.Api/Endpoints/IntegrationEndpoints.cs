using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using WorkBase.Modules.Integration.Application.Commands;
using WorkBase.Modules.Integration.Application.Queries;
using WorkBase.Modules.Integration.Application.Services;
using WorkBase.Modules.Integration.Domain.Enums;
using WorkBase.Shared.Api;
using WorkBase.Shared.Auth;

namespace WorkBase.Modules.Integration.Api.Endpoints;

public static class IntegrationEndpoints
{
    public static IEndpointRouteBuilder MapIntegrationEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/integrations")
            .WithTags("Integrations")
            .RequireAuthorization();

        // --- Connections ---
        group.MapGet("/connections", GetConnections)
            .WithName("GetIntegrationConnections")
            .WithSummary("Pobierz połączenia integracyjne użytkownika")
            .Produces<List<ConnectionDto>>();

        group.MapPost("/connect", ConnectProvider)
            .WithName("ConnectProvider")
            .WithSummary("Połącz z zewnętrznym dostawcą (OAuth callback)")
            .Produces<Guid>(StatusCodes.Status201Created);

        group.MapPost("/disconnect", DisconnectProvider)
            .WithName("DisconnectProvider")
            .WithSummary("Rozłącz z zewnętrznym dostawcą");

        // --- OAuth URLs ---
        group.MapGet("/auth-url", GetAuthUrl)
            .WithName("GetIntegrationAuthUrl")
            .WithSummary("Pobierz URL autoryzacji OAuth dla dostawcy")
            .Produces<AuthUrlResponse>();

        // --- Calendar ---
        group.MapPost("/calendar/push-leave", PushLeaveToCalendar)
            .WithName("PushLeaveToCalendar")
            .WithSummary("Wyślij urlop do kalendarza zewnętrznego");

        return endpoints;
    }

    private static async Task<IResult> GetConnections(ClaimsPrincipal user, ISender sender)
    {
        var userId = Guid.Parse(user.FindFirstValue("sub")!);
        var result = await sender.Send(new GetUserConnectionsQuery(userId));
        return result.ToHttpResult();
    }

    private static async Task<IResult> ConnectProvider(ConnectProviderBody body, ISender sender)
    {
        var result = await sender.Send(new ConnectProviderCommand(body.Provider, body.Code, body.RedirectUri));
        return result.IsSuccess
            ? Results.Created($"/api/integrations/connections/{result.Value}", result.Value)
            : result.ToHttpResult();
    }

    private static async Task<IResult> DisconnectProvider(DisconnectProviderBody body, ISender sender)
    {
        var result = await sender.Send(new DisconnectProviderCommand(body.Provider));
        return result.ToHttpResult();
    }

    private static IResult GetAuthUrl(
        IntegrationProvider provider, string redirectUri, ClaimsPrincipal user, IOAuthFlowService oAuthFlowService)
    {
        var userId = Guid.Parse(user.FindFirstValue("sub")!);
        var tenantId = Guid.Parse(user.FindFirstValue("tenant_id")!);

        var url = oAuthFlowService.GetAuthorizationUrl(provider, tenantId, userId, redirectUri);
        return Results.Ok(new AuthUrlResponse(url));
    }

    private static async Task<IResult> PushLeaveToCalendar(PushLeaveToCalendarBody body, ISender sender)
    {
        var result = await sender.Send(new PushLeaveToCalendarCommand(
            body.Provider, body.EmployeeName, body.LeaveType, body.StartDate, body.EndDate));
        return result.ToHttpResult();
    }
}

// Request body records
public sealed record ConnectProviderBody(IntegrationProvider Provider, string Code, string RedirectUri);
public sealed record DisconnectProviderBody(IntegrationProvider Provider);
public sealed record AuthUrlResponse(string Url);
public sealed record PushLeaveToCalendarBody(
    IntegrationProvider Provider, string EmployeeName, string LeaveType,
    DateTime StartDate, DateTime EndDate);

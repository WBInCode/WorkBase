using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using WorkBase.Modules.TimeTracking.Application.Commands;
using WorkBase.Shared.Api;
using WorkBase.Shared.Auth;

namespace WorkBase.Modules.TimeTracking.Api.Endpoints;

public static class NfcEndpoints
{
    public static IEndpointRouteBuilder MapNfcEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/time/nfc")
            .WithTags("NFC")
            .RequireAuthorization();

        group.MapPost("/badges", async (RegisterNfcBadgeBody body, ISender sender) =>
        {
            var cmd = new RegisterNfcBadgeCommand(body.EmployeeId, body.BadgeUid, body.Label);
            var result = await sender.Send(cmd);
            return result.IsSuccess
                ? Results.Created($"/api/time/nfc/badges/{result.Value}", result.Value)
                : result.ToHttpResult();
        })
        .WithName("RegisterNfcBadge")
        .WithSummary("Zarejestruj identyfikator NFC dla pracownika")
        .RequirePermission("time.manage")
        .Produces<Guid>(StatusCodes.Status201Created);

        group.MapPost("/clock-in", async (NfcClockInBody body, ISender sender) =>
        {
            var cmd = new NfcClockInCommand(body.BadgeUid);
            var result = await sender.Send(cmd);
            return result.ToHttpResult();
        })
        .WithName("NfcClockIn")
        .WithSummary("Rejestracja wejścia/wyjścia przez NFC")
        .RequirePermission("time.create")
        .Produces<Guid>();

        return endpoints;
    }
}

public sealed record RegisterNfcBadgeBody(Guid EmployeeId, string BadgeUid, string? Label);
public sealed record NfcClockInBody(string BadgeUid);
